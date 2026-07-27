using System.ComponentModel;

namespace WayfarerAPI.Domain.Enumerations
{
    public enum ExpenseCategoryEnum
    {
        [Description("交通")]
        Transportation,
        [Description("住宿")]
        Accommodation,
        [Description("餐飲")]
        FoodOrDrink,
        [Description("超市採購")]
        Groceries,
        [Description("購物")]
        Shopping,
        [Description("景點娛樂")]
        Attraction,
        [Description("其他")]
        Other
    }
}
