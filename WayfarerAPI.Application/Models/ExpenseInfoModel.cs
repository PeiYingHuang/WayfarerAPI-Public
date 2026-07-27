using WayfarerAPI.Domain.Enumerations;

namespace WayfarerAPI.Application.Models
{
    public class ExpenseInfoModel
    {
        public DateTime ExpenseTime { get; set; }
        public ExpenseCategoryEnum Category { get; set; } = ExpenseCategoryEnum.Other;
        public string ItemName { get; set; } = string.Empty;
        public decimal SplitAmount { get; set; }
    }
}
