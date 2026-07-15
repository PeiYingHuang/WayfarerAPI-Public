using System.ComponentModel;

namespace WayfarerAPI.Domain.Enumerations;

public enum GenderEnum
{
    [Description("男性")]
    Male = 1,
    [Description("女性")]
    Female = 2,
    [Description("其他")]
    Other = 3
}
