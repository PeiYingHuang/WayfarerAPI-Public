using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using WayfarerAPI.Application.Interfaces.Utilities;
using WayfarerAPI.Application.Models;
using WayfarerAPI.Domain.Enumerations;

namespace WayfarerAPI.Infrastructure.Utilities;

public sealed class OpenAiVisionClient : IOpenAiVisionClient
{
    private static readonly HttpClient HttpClient = new();
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenAiVisionClient> _logger;
    public OpenAiVisionClient(IConfiguration configuration, ILogger<OpenAiVisionClient> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    #region Ocr
    private const string MerchantAndDescriptionRules = """
        MERCHANT NAME — apply these rules in order, stop at the first match:
 
        1. If the merchant has an official or widely used Traditional Chinese name in the
           Taiwan/Hong Kong market, output that name.
             'ユニクロ' → '優衣庫'      '롯데마트' → '樂天超市'
             'イオン'   → '永旺'        '이마트'   → '易買得'
             '無印良品' → '無印良品'    '다이소'   → '大創'
             'スターバックス' → '星巴克' '올리브영' → 'Olive Young'
 
        2. Otherwise, if the merchant's internationally recognized brand name is written in
           Latin characters, output that Latin name unchanged. Do not translate it.
             'ZARA' → 'ZARA'    'Carrefour' → 'Carrefour'    'IKEA' → 'IKEA'
 
        3. Otherwise, transliterate phonetically into Traditional Chinese characters.
           Never copy the original Kana or Hangul.
 
        BRANCH AND LEGAL SUFFIXES:
        - Translate branch suffixes: '〜店' stays '〜店', '〜점' → '〜店'.
          Place names inside branch suffixes follow the rules above ('해운대점' → '海雲臺店').
        - Remove legal-entity and franchise codes in parentheses, e.g. '(에스에스엠)',
          '(株)', '(주)', '(有)'.
        - Example: '미피 해운대점(에스에스엠)' → 'Miffy 海雲臺店'
 
        HARD CONSTRAINT:
        merchantName must never contain Hiragana, Katakana, or Hangul.
        Latin letters are allowed only under rule 2.
 
        DESCRIPTION FIELD:
        For each item, provide a concise Traditional Chinese label. If the brand is
        recognizable, include the brand followed by the product type in Chinese, plus
        size/quantity when meaningful. If the brand is not recognizable, give the product
        type in Chinese only.
 
        CRITICAL:
        The description must not contain Hiragana, Katakana, Hangul, Cyrillic, or any other
        non-Latin, non-Chinese script. Traditional Chinese characters, Arabic numerals,
        units, and recognizable Latin-character brand names are allowed. If a proper noun
        cannot be confidently translated, transliterate it into Chinese characters rather
        than copying the original script.
 
        Examples (French):
          'CONFITURE BM FRAISE 370G'      → 'Bonne Maman 草莓果醬 370g'
          'CRISTALINE EAU DE SOURCE 5L'   → 'Cristaline 礦泉水 5公升'
          'POULAIN 1848 PISTACHE CROUSTI' → 'Poulain 1848 開心果巧克力'
          'PAIN WRAP X6 - 370G MR'        → '捲餅 x6'
          'KIWI GOLD'                     → '黃金奇異果'
          'SAUCE SOJA CARAFE X150ML'      → '醬油'
 
        Examples (Korean):
          '인형 보리스 해바라기'  → '波里斯 向日葵娃娃'
          '해운대 쇼핑백 S'       → '海雲臺購物袋 S號'
          '아메리카노 ICE'        → '美式咖啡 冰'
          '삼각김밥 참치마요'     → '飯糰 鮪魚美乃滋'
 
        Examples (Japanese):
          'ラムネ 大瓶'           → '彈珠汽水 大瓶'
          'おにぎり 鮭'           → '飯糰 鮭魚'
          'コカ・コーラ 500ml'    → '可口可樂 500ml'
        """;
    public async Task<ReceiptModel> ParseReceiptAsync(byte[] imageBytes, string mimeType, string? currency = null)
    {
        var apiKey = _configuration["OpenAI:ApiKey:OCR"];
        var model = _configuration["OpenAI:OcrModel"];

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("未設定 OpenAI:ApiKey:OCR");

        if (string.IsNullOrWhiteSpace(model))
            model = "gpt-4.1-mini";

        var base64 = Convert.ToBase64String(imageBytes);
        var dataUrl = $"data:{mimeType};base64,{base64}";
        var currencyHint = string.IsNullOrWhiteSpace(currency)
            ? "If currency is not specified, infer from receipt text/symbols if possible."
            : $"Use currency '{currency}' as the primary interpretation context for price fields unless the receipt explicitly indicates another currency.";

        var systemPrompt = $"""
            You are a multilingual receipt OCR extractor for Traditional Chinese, Japanese,
            Korean, and European receipts.
            Return ONLY strict JSON with keys: merchantName, consumedAt, totalAmount, items.
            items must be an array of objects with keys: name, quantity, amount, description.
            If unknown, use null (or [] for items).
            Prioritize total amount labels such as: 合計/總計/應付/總金額, 合計(税込)/お会計/請求額,
            합계/총계/결제금액, total/grand total/amount due.
            Do not include markdown or extra text.
 
            Determine the receipt's language before extracting merchantName.
 
            {MerchantAndDescriptionRules}
 
            DATES:
            European receipts (France, etc.) use dd/MM/yy or dd/MM/yyyy — day first, then
            month, then year. Example: '28/05/26' means day=28, month=05, year=2026 → '2026-05-28'.
            Never treat the first component as year unless it is clearly 4 digits.
            Always output consumedAt in ISO 8601 format (YYYY-MM-DDTHH:mm:ss) when possible.
            """;

        var payload = new
        {
            model,
            response_format = new { type = "json_object" },
            messages = new object[]
            {
                new
                {
                    role = "system",
                    content = systemPrompt
                },
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new
                        {
                            type = "text",
                            text = $"""
                                請解析收據（支援英文/中文/日文/韓文/法文）：
                                1) 店家名稱 -> merchantName
                                2) 消費時間 -> consumedAt（ISO 格式 YYYY-MM-DDTHH:mm:ss）
                                3) 總金額   -> totalAmount（優先抓真正總計，不要抓小計或稅額）
                                4) 明細     -> items[]，每筆含：
                                   - name：收據上的原始品項名稱（保留原文，不翻譯）
                                   - quantity：數量
                                   - amount：金額
                                   - description：繁體中文品項說明
                                若欄位不存在請回 null；items 沒有就回空陣列。只回傳 JSON。
                                {currencyHint}
                                """
                        },
                        new
                        {
                            type = "image_url",
                            image_url = new { url = dataUrl }
                        }
                    }
                }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await HttpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        _logger.LogInformation("ocr responseBody: {0}", responseBody);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"OpenAI Vision API 錯誤：{response.StatusCode}, {responseBody}");

        var apiResponse = JsonSerializer.Deserialize<OpenAiOcrModel>(responseBody)
            ?? throw new InvalidOperationException("無法反序列化 OpenAI Vision API 回應");

        var content = apiResponse.Choices
            .FirstOrDefault()?.Message.Content
            ?? throw new InvalidOperationException("OpenAI Vision API 未回傳可解析內容");

        var receipt = JsonSerializer.Deserialize<ReceiptModel>(content)
             ?? throw new InvalidOperationException("無法解析發票內容");

        FixConsumedAtDate(receipt);

        //// Prompt 無法 100% 保證，偵測到殘留假名/諺文就跑一次純文字修補（不重傳圖片，成本低）
        //if (NeedsRepair(receipt))
        //    await RepairScriptAsync(receipt, apiKey, model);
        return receipt;
    }

    ///// <summary>
    ///// 偵測殘留的假名 / 諺文。
    ///// 注意：刻意不包含漢字範圍 (U+4E00–9FFF)，因為「無印良品」這類日文店名本來就該原樣保留。
    ///// 簡體字此處也抓不到，若有需求需另外對照簡繁對映表。
    ///// </summary>
    //private static readonly Regex NonChineseScript = new(
    //    @"[\u3040-\u309F\u30A0-\u30FF\uFF66-\uFF9D\u1100-\u11FF\u3130-\u318F\uAC00-\uD7A3]",
    //    RegexOptions.Compiled);
    //private static bool NeedsRepair(ReceiptModel receipt) =>
    //    NonChineseScript.IsMatch(receipt.MerchantName ?? string.Empty) ||
    //    (receipt.Items?.Any(i => NonChineseScript.IsMatch(i.Description ?? string.Empty)) ?? false);

    ///// <summary>
    ///// 只把 merchantName 與 description 送去重譯，保留 name / quantity / amount 不動。
    ///// 失敗時不拋例外——修補是盡力而為，原始結果仍可用。
    ///// </summary>
    //private async Task RepairScriptAsync(ReceiptModel receipt, string apiKey, string model)
    //{
    //    var items = receipt.Items ?? new List<ReceiptItemModel>();

    //    var toRepair = new
    //    {
    //        merchantName = receipt.MerchantName,
    //        items = items
    //            .Select((item, index) => new { index, name = item.Name, description = item.Description })
    //            .ToArray()
    //    };

    //    var payload = new
    //    {
    //        model,
    //        response_format = new { type = "json_object" },
    //        messages = new object[]
    //        {
    //            new
    //            {
    //                role = "system",
    //                content = $"""
    //                    You fix Traditional Chinese localization in already-extracted receipt data.
    //                    Input is JSON. Return ONLY JSON with the same shape:
    //                    {{ "merchantName": string|null, "items": [ {{ "index": int, "description": string|null }} ] }}
    //                    Keep the index values exactly as given. Do not add or remove items.
    //                    Do not change any numbers.

        //                    {MerchantAndDescriptionRules}
        //                    """
        //            },
        //            new
        //            {
        //                role = "user",
        //                content = JsonSerializer.Serialize(toRepair)
        //            }
        //        }
        //    };

        //    try
        //    {
        //        var repaired = await SendOcrRequestAsync<ReceiptRepairModel>(payload, apiKey, "無法解析修補結果");

        //        if (!string.IsNullOrWhiteSpace(repaired.MerchantName))
        //            receipt.MerchantName = repaired.MerchantName;

        //        foreach (var fix in repaired.Items ?? new List<ReceiptRepairItemModel>())
        //        {
        //            if (fix.Index < 0 || fix.Index >= items.Count)
        //                continue;

        //            if (!string.IsNullOrWhiteSpace(fix.Description))
        //                items[fix.Index].Description = fix.Description;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        // 修補失敗不影響主流程，保留原始 OCR 結果
        //        _logger.LogWarning(ex, "收據文字修補失敗，沿用原始 OCR 結果");
        //    }
        //}

    private static void FixConsumedAtDate(ReceiptModel receipt)
    {
        if (receipt.ConsumedAt is null)
            return;

        var parsed = receipt.ConsumedAt.Value;
        var currentYear = DateTime.UtcNow.Year;

        // 年份在合理範圍內，不需修正
        if (parsed.Year <= currentYear + 1)
            return;

        // 嘗試以歐式格式重新解析（AI 可能誤把 day 當作年份）
        // 例如：2028-05-26 應為 2026-05-28
        var wrongYear = parsed.Year % 100;
        var reconstructed = $"{parsed.Day:D2}/{parsed.Month:D2}/{wrongYear:D2}";

        string[] europeanFormats = ["dd/MM/yy", "dd/MM/yyyy", "d/M/yy", "d/M/yyyy"];

        foreach (var fmt in europeanFormats)
        {
            if (DateTime.TryParseExact(reconstructed, fmt,
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var corrected))
            {
                // 保留原本的時間部分，只替換日期
                receipt.ConsumedAt = corrected.Date + parsed.TimeOfDay;
                return;
            }
        }
    }

    private static string ExtractJson(string content)
    {
        var trimmed = content.Trim();
        if (trimmed.StartsWith("```"))
        {
            var firstBrace = trimmed.IndexOf('{');
            var lastBrace = trimmed.LastIndexOf('}');
            if (firstBrace >= 0 && lastBrace > firstBrace)
                return trimmed[firstBrace..(lastBrace + 1)];
        }

        return trimmed;
    }

    private static readonly string CategoryOptions = string.Join(", ", Enum.GetNames<ItineraryCategoryEnum>().Select(name => ToPromptValue(name)));

    private static string ToPromptValue(string enumName)
    {
        // TrainStation -> train station
        return string.Concat(enumName.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? " " + char.ToLower(c) : char.ToLower(c).ToString()));
    }

    private static readonly Regex UntranslatedScriptPattern =
    new(@"[\uAC00-\uD7A3\u3040-\u30FF]", RegexOptions.Compiled);

    /// <summary>
    /// 判斷字串中是否含有韓文或日文，若有則回傳 true
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    private static bool ContainsUntranslatedScript(string? text) =>
        !string.IsNullOrEmpty(text) && UntranslatedScriptPattern.IsMatch(text);
    #endregion

    private static readonly Dictionary<string, ItineraryCategoryEnum> CategoryLookup =
    Enum.GetValues<ItineraryCategoryEnum>()
        .ToDictionary(value => ToPromptValue(value.ToString()), value => value);
    public async Task<ItineraryCategoryEnum> ParseCategory(string? promptValue)
    {
        if (promptValue is not null &&
            CategoryLookup.TryGetValue(promptValue.Trim().ToLowerInvariant(), out var result))
        {
            return result;
        }

        return ItineraryCategoryEnum.Other;
    }


    private static readonly string ItinerarySystemPrompt = BuildSystemPrompt();
    private static string BuildSystemPrompt()
    {
        const string template = """
                    你是專業旅遊行程規劃師，熟悉全球主要旅遊城市的景點、交通方式、營業時間、文化習慣與在地美食。
                    請根據使用者提供的目的地、天數、人數組成、班機資訊與指定景點/活動需求，規劃每日行程，並適當安排在地美食推薦。

                    輸出規則：
                    - 只回傳 JSON，不要包含 markdown 或任何額外文字。
                    - category 只能從此清單選擇：#CategoryOptions#。
                    - 若使用者有指定的景點/活動，務必安排進行程中，不可省略。
                    - 若有去程班機，第一天行程須安排在班機抵達時間之後，並預留至少 1 小時的機場往市區交通時間。
                    - 若有回程班機，最後一天行程須在起飛前至少 3 小時結束，並預留機場交通時間。
                    - 有小孩同行（尤其學齡前）時，避免安排單日超過 2 個高強度長時間步行景點，行程間預留休息與用餐時間。
                    - 每個景點的 startTime/endTime 請合理估算所需時間，避免時間重疊或行程過於緊湊。
                    - 每日需安排至少 1 次午餐、1 次晚餐，餐廳選擇需貼近當日行程動線，避免繞路。
                    - 餐廳/店家名稱須為真實存在或高知名度店家；若不確定確切店名，改用描述性用語（如「當地知名牛肉麵店」），不可捏造店名或地址。
                    - 餐廳推薦需在 description 簡述推薦理由（人氣、特色、適合親子等）。

                    JSON 格式如下：
                    {
                      "days": [
                        {
                          "dayNumber": 1,
                          "date": "yyyy-MM-dd",
                          "dayTitle": "string",
                          "details": [
                            {
                              "title": "string",
                              "description": "string or null",
                              "startTime": "HH:mm or null",
                              "endTime": "HH:mm or null",
                              "locationName": "string or null",
                              "category": "string"
                            }
                          ]
                        }
                      ]
                    }
                    """;
        return template.Replace("#CategoryOptions#", CategoryOptions, StringComparison.Ordinal);
    }

    public async Task<AiItineraryDraftModel> GenerateItineraryAsync(ItineraryAiModel request, CancellationToken ct)
    {
        var apiKey = _configuration["OpenAI:ApiKey:AI"];
        var model = _configuration["OpenAI:ItineraryModel"];

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("未設定 OpenAI:ApiKey:AI");

        if (string.IsNullOrWhiteSpace(model))
            model = "gpt-4.1-mini";

        var userPrompt = BuildUserPrompt(request);
        _logger.LogInformation("生成的使用者提示：{Prompt}", userPrompt);

        var payload = new
        {
            model,
            response_format = new { type = "json_object" },
            messages = new object[]
            {
            new { role = "system", content = ItinerarySystemPrompt },
            new { role = "user", content = userPrompt }
            }
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        httpRequest.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await HttpClient.SendAsync(httpRequest, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"OpenAI API 錯誤：{response.StatusCode}, {responseBody}");

        using var doc = JsonDocument.Parse(responseBody);
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrWhiteSpace(content))
            throw new InvalidOperationException("OpenAI API 未回傳可解析內容");

        var json = ExtractJson(content);

        try
        {
            var draft = JsonSerializer.Deserialize<AiItineraryDraftModel>(json, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

            if (draft is null || draft.Days.Count == 0)
                throw new InvalidOperationException("AI 回傳的行程為空");

            return draft;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "AI 回傳的 JSON 無法解析：{Json}", json);
            throw new InvalidOperationException("AI 回傳格式錯誤，無法解析為行程資料", ex);
        }
    }

    private static string BuildUserPrompt(ItineraryAiModel request)
    {
        var flightSection = new StringBuilder();
        if (request.OutboundFlight != null)
        {
            flightSection.AppendLine(
                $"去程班機：({request.OutboundFlight.DepartureAirport}){request.OutboundFlight.DepartureAt:yyyy-MM-dd HH:mm} → ({request.OutboundFlight.ArrivalAirport}){request.OutboundFlight.ArrivalAt:yyyy-MM-dd HH:mm}");
        }
        if (request.ReturnFlight != null)
        {
            flightSection.AppendLine(
                $"回程班機：({request.ReturnFlight.DepartureAirport}){request.ReturnFlight.DepartureAt:yyyy-MM-dd HH:mm} → ({request.ReturnFlight.ArrivalAirport}){request.ReturnFlight.ArrivalAt:yyyy-MM-dd HH:mm}");
        }
        if(flightSection.Length == 0)
        {
            flightSection.AppendLine("無班機資訊");
        }
        var childrenSection = new StringBuilder();
        if (request.Children.Count > 0)
        {
            childrenSection.AppendLine($"小孩：");
            foreach (var (age, count) in request.Children)
            {
                childrenSection.AppendLine(age > 0
                    ? $"{count} 位，年齡 {age} 歲"
                    : $"{count} 位");
            }
        }

        var totalDays = (request.EndDate - request.StartDate).Days + 1;

        return $"""
        目的地：{request.Destination}
        旅遊天數：共 {totalDays} 天
        大人：{request.AdultCount}位
        {childrenSection}
        {flightSection}

        使用者提供的資訊，請務必加入參考：
        {request.UserPreferences}

        請規劃 {totalDays} 天的完整行程。
        """;
    }
}
