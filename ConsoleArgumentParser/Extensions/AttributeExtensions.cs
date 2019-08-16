using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ConsoleArgumentParser.Extensions
{
    public static class AttributeExtensions
    {
        public static TValue GetAttributeValue<TAttribute, TValue>(this ICustomAttributeProvider provider, Func<TAttribute, TValue> valueSelector)
            where TAttribute : Attribute
        {
            if (provider.GetCustomAttributes(typeof(TAttribute), true).FirstOrDefault() is TAttribute att)
            {
                return valueSelector(att);
            }
            return default;
        }

        public static TAttribute GetAttribute<TAttribute>(this ICustomAttributeProvider provider) where TAttribute : Attribute
        {
            if (provider.GetCustomAttributes(typeof(TAttribute), true).FirstOrDefault() is TAttribute att)
            {
                return att;
            }

            return default;
        }

        public static IEnumerable<MethodInfo> GetSubCommands(this Type command)
        {
            return command.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(m => m.GetCustomAttributes(typeof(CommandArgumentAttribute), true).Length > 0);
        }
    }
}