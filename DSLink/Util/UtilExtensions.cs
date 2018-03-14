using System;
using Castle.MicroKernel;
using Newtonsoft.Json.Linq;

namespace DSLink.Util
{
    public static class UtilExtensions
    {
        public static dynamic ToDynamic(this JToken jtoken)
        {
            switch (jtoken.Type)
            {
                case JTokenType.Boolean:
                    return jtoken.Value<bool>();
                case JTokenType.Bytes:
                    return jtoken.Value<byte[]>();
                case JTokenType.Float:
                    return jtoken.Value<float>();
                case JTokenType.Integer:
                    return jtoken.Value<int>();
                case JTokenType.String:
                    return jtoken.Value<string>();
                default:
                    return null;
            }
        }

        public static string ToIso8601(this DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz");
        }

        public static bool HasComponent<T>(this IKernel kernel)
        {
            return kernel.HasComponent(typeof(T));
        }
    }
}
