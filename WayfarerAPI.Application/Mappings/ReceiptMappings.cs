using WayfarerAPI.Application.DTOs;
using WayfarerAPI.Application.Models;

namespace WayfarerAPI.Application.Mappings
{
    public static class ReceiptMappings
    {
        public static OcrReceiptDto ToDto(ReceiptModel receipt)
        {
            var receiptDto = new OcrReceiptDto() 
            {
                MerchantName = receipt.MerchantName,
                ConsumedAt = receipt.ConsumedAt?.ToString("yyyy-MM-dd HH:mm"),
                TotalAmount = receipt.TotalAmount
            };
            foreach(var i in receipt.Items)
            {
                receiptDto.Items.Add(new OcrReceiptItemDto()
                {
                    Name = i.Name,
                    Quantity = i.Quantity,
                    Amount = i.Amount,
                    Description = i.Description ?? ""
                });
            }
            return receiptDto;
        }
    }
}
