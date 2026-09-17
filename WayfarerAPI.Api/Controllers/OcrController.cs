using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WayfarerAPI.Application.DTOs;
using WayfarerAPI.Application.Interfaces.Service;

namespace WayfarerAPI.Api.Controllers;

[Route("api/ocr")]
[Authorize]
public class OcrController : ApiControllerBase
{
    private readonly IOcrService _ocrService;
    private readonly ILogger<OcrController> _logger;

    public OcrController(IOcrService ocrService, ILogger<OcrController> logger)
    {
        _ocrService = ocrService;
        _logger = logger;
    }

    [HttpPost("receipt")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ParseReceipt([FromForm] string? currency = null)
    {
        var files = Request.Form.Files;
        if (files == null || files.Count == 0)
            return ApiResult(false, "請上傳圖片");

        var combined = new OcrReceiptDto
        {
            MerchantName = null,
            ConsumedAt = null,
            TotalAmount = 0m,
            Items = new List<OcrReceiptItemDto>(),
        };

        foreach (var file in files)
        {
            if (file == null || file.Length == 0)
                continue;

            byte[] imageBytes;
            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                imageBytes = ms.ToArray();
            }

            var result = await _ocrService.ParseReceiptAsync(imageBytes, file.ContentType, currency);
            if (result == null)
                continue;

            if (!string.IsNullOrWhiteSpace(result.MerchantName))
            {
                combined.MerchantName = combined.MerchantName is null
                    ? result.MerchantName
                    : $"{combined.MerchantName} / {result.MerchantName}";
            }

            if (combined.ConsumedAt is null && !string.IsNullOrWhiteSpace(result.ConsumedAt))
            {
                combined.ConsumedAt = result.ConsumedAt;
            }

            if (result.TotalAmount.HasValue)
            {
                combined.TotalAmount += result.TotalAmount.Value;
            }

            if (result.Items != null && result.Items.Count > 0)
            {
                combined.Items.AddRange(result.Items);
            }
        }

        if (combined.TotalAmount == 0m && combined.Items.Count == 0)
            return ApiResult(false, "無法辨識收據內容");

        _logger.LogInformation(JsonConvert.SerializeObject(combined));

        //var combined = new OcrReceiptDto
        //{
        //    MerchantName = "Miffy 海雲臺",
        //    ConsumedAt = "2026-05-29 16:47",
        //    TotalAmount = 65500,
        //    Items = [
        //        new OcrReceiptItemDto { Name="인형 보리스 해바라기",
        //            Quantity=1,
        //            Amount=64000,
        //            Description="波里斯 向日葵娃娃"
        //        },
        //        new OcrReceiptItemDto
        //        {Name="해운대 쇼핑백S",Quantity=1,Amount=1500,Description="海雲臺購物袋 S號"}
        //    ]
        //};

        //    var combined = new OcrReceiptDto
        //    {
        //        MerchantName = "E.Leclerc",
        //        ConsumedAt = "2026-05-29 16:47",
        //        TotalAmount = 84.01m,
        //        Items =
        //[
        //    new OcrReceiptItemDto
        //    {
        //        Name = "PALMITO X1",
        //        Quantity = 1.0m,
        //        Amount = 0.84m,
        //        Description = "棕櫚心 1件"
        //    },
        //    new OcrReceiptItemDto
        //    {
        //        Name = "CONFITURE BM FRAISE 370G",
        //        Quantity = 1.0m,
        //        Amount = 1.98m,
        //        Description = "Bonne Maman 草莓果醬 370g"
        //    },
        //    new OcrReceiptItemDto
        //    {
        //        Name = "BM CONFITURE FRAMBOISE 370G",
        //        Quantity = 1.0m,
        //        Amount = 2.55m,
        //        Description = "Bonne Maman 鈴莓果醬 370g"
        //    },
        //    new OcrReceiptItemDto
        //    {
        //        Name = "CACAHUETES GRILLEES SALEES 450",
        //        Quantity = 1.0m,
        //        Amount = 3.75m,
        //        Description = "鹽烤花生 450g"
        //    },
        //    new OcrReceiptItemDto
        //    {
        //        Name = "MAIS DOUX 285G PNE ECO+",
        //        Quantity = 1.0m,
        //        Amount = 0.73m,
        //        Description = "甜玉米 285g"
        //    },
        //    new OcrReceiptItemDto
        //    {
        //        Name = "CHIPS PAYSANNE 150G MR",
        //        Quantity = 1.0m,
        //        Amount = 0.99m,
        //        Description = "鄉村風薯片 150g"
        //    },
        //    new OcrReceiptItemDto
        //    {
        //        Name = "PAIN WRAP X6 - 370G MR",
        //        Quantity = 1.0m,
        //        Amount = 1.95m,
        //        Description = "捲餅 x6 370g"
        //    },
        //    new OcrReceiptItemDto
        //    {
        //        Name = "POULAIN 1848 PISTACHE CROUSTI 1",
        //        Quantity = 1.0m,
        //        Amount = 4.41m,
        //        Description = "Poulain 1848 開心果巧克力"
        //    },
        //    new OcrReceiptItemDto
        //    {
        //        Name = "SAUCE SOJA CARAFE X150ML",
        //        Quantity = 1.0m,
        //        Amount = 3.21m,
        //        Description = "醬油 150ml"
        //    },
        //    new OcrReceiptItemDto
        //    {
        //        Name = "MAYO NATURE FLACON 395 G 400 ML",
        //        Quantity = 1.0m,
        //        Amount = 2.49m,
        //        Description = "美乃滋 395g 400ml"
        //    },
        //    new OcrReceiptItemDto
        //    {
        //        Name = "KN MARM BOUIL LEG X8",
        //        Quantity = 1.0m,
        //        Amount = 1.98m,
        //        Description = "Knorr 蔬菜高湯塊 x8"
        //    },
        //    new OcrReceiptItemDto
        //    {
        //        Name = "CRISTALINE EAU DE SOURCE 5L",
        //        Quantity = 1.0m,
        //        Amount = 0.89m,
        //        Description = "Cristaline 礦泉水 5公升"
        //    },
        //    new OcrReceiptItemDto
        //    {
        //        Name = "IGP PAYS D’OC CAB SAUV MR 75CL",
        //        Quantity = 1.0m,
        //        Amount = 2.25m,
        //        Description = "IGP 產地紅酒 75cl"
        //    },
        //    new OcrReceiptItemDto
        //    {
        //        Name = "JUS ORANGE ABC 2L JAFADEN",
        //        Quantity = 1.0m,
        //        Amount = 3.51m,
        //        Description = "ABC 橙汁 2公升"
        //    },
        //    new OcrReceiptItemDto
        //    {
        //        Name = "COCA COLA PET 1,75L CT",
        //        Quantity = 1.0m,
        //        Amount = 2.42m,
        //        Description = "可口可樂 1.75公升 PET瓶"
        //    },
        //    new OcrReceiptItemDto
        //    {
        //        Name = "ALVS PLATI SERVIETTES 28CT",
        //        Quantity = 1.0m,
        //        Amount = 7.37m,
        //        Description = "Alvès 平面紙巾 28片"
        //    },
        //    new OcrReceiptItemDto
        //    {
        //        Name = "OEUFS FRAIS SOL DJP X6 MR",
        //        Quantity = 1.0m,
        //        Amount = 1.52m,
        //        Description = "新鮮蛋 6入"
        //    },
        //    new OcrReceiptItemDto
        //    {
        //        Name = "OEUFS SOL DJP GROS COQUE X6 MR",
        //        Quantity = 1.0m,
        //        Amount = 1.55m,
        //        Description = "大型雞蛋 6入"
        //    },
        //    new OcrReceiptItemDto
        //    {
        //        Name = "PAVE ECHINE S/OS A GRILLER",
        //        Quantity = 1.0m,
        //        Amount = 7.55m,
        //        Description = "去骨豬肩排 烤肉用"
        //    },
        //    new OcrReceiptItemDto
        //    {
        //        Name = "V.BOVINE K7 FAUX FILET***",
        //        Quantity = 1.0m,
        //        Amount = 10.60m,
        //        Description = "牛肉假腰肉"
        //    },
        //    new OcrReceiptItemDto
        //    {
        //        Name = "470G LG AILE POULET BARBECUE S/",
        //        Quantity = 1.0m,
        //        Amount = 3.69m,
        //        Description = "燒烤雞翅 470g"
        //    },
        //    new OcrReceiptItemDto
        //    {
        //        Name = "KIWI GOLD",
        //        Quantity = 6.0m,
        //        Amount = 5.94m,
        //        Description = "黃金奇異果 6個"
        //    },
        //    new OcrReceiptItemDto
        //    {
        //        Name = "POMME BICOLORE",
        //        Quantity = 1.0m,
        //        Amount = 1.65m,
        //        Description = "雙色蘋果"
        //    },
        //    new OcrReceiptItemDto
        //    {
        //        Name = "SALADE MELANGEE 250G ECO+",
        //        Quantity = 1.0m,
        //        Amount = 0.75m,
        //        Description = "綜合沙拉 250g"
        //    },
        //    new OcrReceiptItemDto
        //    {
        //        Name = "COURGETTE FILET KG ECO+",
        //        Quantity = 2.0m,
        //        Amount = 3.18m,
        //        Description = "櫛瓜 2公斤"
        //    },
        //    new OcrReceiptItemDto
        //    {
        //        Name = "OIGNON JAUNE TUBE 500G",
        //        Quantity = 1.0m,
        //        Amount = 1.56m,
        //        Description = "黃洋蔥 500g"
        //    },
        //    new OcrReceiptItemDto
        //    {
        //        Name = "470G LG AILE POULET BARBECUE S/",
        //        Quantity = 2.0m,
        //        Amount = 3.69m,
        //        Description = "雞翅燒烤 470克"
        //    },
        //    new OcrReceiptItemDto
        //    {
        //        Name = "KIWI GOLD 6 X 0.99€",
        //        Quantity = 2.0m,
        //        Amount = 5.94m,
        //        Description = "黃金奇異果 6個"
        //    },
        //    new OcrReceiptItemDto
        //    {
        //        Name = "POMME BICOLORE",
        //        Quantity = 2.0m,
        //        Amount = 1.65m,
        //        Description = "雙色蘋果"
        //    },
        //    new OcrReceiptItemDto
        //    {
        //        Name = "SALADE MELANGEE 250G ECO+",
        //        Quantity = 2.0m,
        //        Amount = 0.75m,
        //        Description = "混合沙拉 250克 ECO+"
        //    },
        //    new OcrReceiptItemDto
        //    {
        //        Name = "COURGETTE FILET KG ECO+ 2 X 1.59€",
        //        Quantity = 2.0m,
        //        Amount = 3.18m,
        //        Description = "ECO+ 節瓜 1公斤"
        //    },
        //    new OcrReceiptItemDto
        //    {
        //        Name = "OIGNON JAUNE TUBE 500G",
        //        Quantity = 2.0m,
        //        Amount = 1.56m,
        //        Description = "黃洋蔥 500克"
        //    },
        //    new OcrReceiptItemDto
        //    {
        //        Name = "AIL BLANC 2 TETES-60/80- FRANCE",
        //        Quantity = 2.0m,
        //        Amount = 2.55m,
        //        Description = "法國白蒜 2頭"
        //    },
        //    new OcrReceiptItemDto
        //    {
        //        Name = "FRAISE BQUE 500G - ESPAGNE",
        //        Quantity = 2.0m,
        //        Amount = 2.49m,
        //        Description = "西班牙草莓 500克"
        //    }
        //]
        //    };

        //    var combined = new OcrReceiptDto
        //    {
        //        MerchantName = "PAK'n SAVE TAUPO",
        //        ConsumedAt = "2025-05-28T15:47",
        //        TotalAmount = 151.22m,
        //        Items = new List<OcrReceiptItemDto>
        //{
        //    new() { Name = "FARMER BROWN COLONY EGGS SIZE 8 6PK", Quantity = null, Amount = 5.39m },
        //    new() { Name = "LINDT EXCELLENCE INTENSE ORANGE 100G", Quantity = null, Amount = 7.00m },
        //    new() { Name = "PAMS PASSATA 680G", Quantity = null, Amount = 2.99m },
        //    new() { Name = "PAMS SOUR PARTY MIX 180G", Quantity = null, Amount = 1.99m },
        //    new() { Name = "PASTA MARIA SPAGHETTI 400G", Quantity = null, Amount = 1.29m },
        //    new() { Name = "WHITTAKERS MINI SLAB PEANUT 180G", Quantity = 3.0m, Amount = 20.07m },
        //    new() { Name = "WHITTAKERS MINI SLAB ALMOND GOLD 180G", Quantity = 9.0m, Amount = 60.21m },
        //    new() { Name = "WHITTAKERS MINI SLAB CREAMY MILK 180G", Quantity = null, Amount = 6.69m },
        //    new() { Name = "COURGETTES", Quantity = 1.045m, Amount = 9.29m },
        //    new() { Name = "PAMS BAGGED SALAD CRISPY 300G", Quantity = null, Amount = 5.99m },
        //    new() { Name = "ALMONDS ROASTED SALTED", Quantity = 0.185m, Amount = 5.46m },
        //    new() { Name = "PEANUTS ROASTED SALTED", Quantity = 0.230m, Amount = 2.73m },
        //    new() { Name = "PAMS CHEESE TASTY SLICES 250G", Quantity = null, Amount = 3.19m },
        //    new() { Name = "PAMS VALUE MILK STANDARD 1L", Quantity = null, Amount = 2.94m },
        //    new() { Name = "PNS HOT COOKED CHICKEN XL", Quantity = null, Amount = 15.99m }
        //}
        //    };

        return ApiResult(combined);
    }
}
