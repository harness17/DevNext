using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dev.CommonLibrary.Extensions
{
    public static class ObjectExtensions
    {
        public static T? Clone<T>(this T source) where T : class
        {
            var config = new MapperConfiguration(cfg => cfg.CreateMap<T, T>(), NullLoggerFactory.Instance);
            return config.CreateMapper().Map<T, T>(source);
        }

        public static string? ToStringOrDefault(this object? value, string? format = null)
        {
            if (value == null) return null;
            if (format != null && value is IFormattable f) return f.ToString(format, null);
            return value.ToString();
        }

        public static string ToStringOrEmpty(this object? value, string? format = null)
        {
            return ToStringOrDefault(value, format) ?? string.Empty;
        }
    }
}
