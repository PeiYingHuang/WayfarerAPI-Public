# Copilot Instructions

## 專案指導方針
- DTO 檔案應放在 Application 層，不應依賴 ASP.NET Core 特定類型如 IFormFile。Controller 層的 Form 請求模型應單獨定義在 Controller 檔案中。