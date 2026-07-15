namespace WayfarerAPI.Domain.Entities;

public class ExpenseDetailSplit
{
    public int Id { get; set; }
    public int ExpenseDetailId { get; set; }
    public Guid MemberId { get; set; }
    public decimal SplitAmount { get; set; }
}
