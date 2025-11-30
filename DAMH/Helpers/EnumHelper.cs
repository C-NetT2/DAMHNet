using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace DAMH.Helpers
{
    public static class EnumHelper
    {
        public static string GetName(this Enum enumValue)
        {
            try
            {
                return enumValue.GetType()
                            .GetMember(enumValue.ToString())
                            .First()
                            .GetCustomAttribute<DisplayAttribute>()?
                            .Name ?? enumValue.ToString();
            }
            catch
            {
                return enumValue.ToString();
            }
        }
    }
}