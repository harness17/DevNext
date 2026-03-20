using Dev.CommonLibrary.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Dev.CommonLibrary.Extensions
{
    public static class EnumExtensions
    {
        public static string? DisplayName(this Enum value)
        {
            var type = value.GetType();
            var field = type.GetField(value.ToString());
            if (field == null) return value.ToString();
            var attr = field.GetCustomAttributes(typeof(DisplayAttribute), false) as DisplayAttribute[];
            if (attr == null || attr.Length == 0) return value.ToString();
            return attr[0].ResourceType != null ? attr[0].GetName() : attr[0].Name;
        }

        public static string? DisplayDescription(this Enum value)
        {
            var type = value.GetType();
            var field = type.GetField(value.ToString());
            if (field == null) return null;
            var attr = field.GetCustomAttributes(typeof(DisplayAttribute), false) as DisplayAttribute[];
            if (attr == null || attr.Length == 0) return null;
            return attr[0].Description;
        }

        public static T? ToSubValue<T>(this Enum value)
        {
            var type = value.GetType();
            var field = type.GetField(value.ToString());
            if (field == null) return default;
            var attrs = field.GetCustomAttributes(typeof(SubValueAttribute), false) as SubValueAttribute[];
            if (attrs == null || attrs.Length == 0) return default;
            return (T)Convert.ChangeType(attrs[0].SubValue, typeof(T));
        }
    }
}
