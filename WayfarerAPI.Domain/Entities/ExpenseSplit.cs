namespace WayfarerAPI.Domain.Entities;

public class ExpenseSplit
{
    public Guid Id { get; set; }
    public Guid ExpenseId { get; set; }
    public Guid MemberId { get; set; }
    public decimal SplitAmount { get; set; }
}
