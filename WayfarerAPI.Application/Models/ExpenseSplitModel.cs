namespace WayfarerAPI.Application.Models
{
    public class ExpenseSplitModel
    {
        public Guid Id { get; set; }
        public Guid ExpenseId { get; set; }
        public Guid MemberId { get; set; }
        public decimal SplitAmount { get; set; }
        public Guid PayerMemberId { get; set; }
    }
}
