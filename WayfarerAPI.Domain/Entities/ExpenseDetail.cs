namespace WayfarerAPI.Domain.Entities;

public class ExpenseDetail
{
    public int Id { get; set; }
    public Guid ExpenseId { get; set; }
    public string Item { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
}
