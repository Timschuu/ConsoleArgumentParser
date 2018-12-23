using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ConsoleArgumentParser.Extensions
{
    public static class AttributeExtensions
    {
        public static TValue GetAttributeValue<TAttribute, TValue>(this Type type, Func<TAttribute, TValue> valueSelector) where TAttribute : Attribute
        {
            if (type.GetCustomAttributes(typeof(TAttribute), true).FirstOrDefault() is TAttribute att)
            {
                return valueSelector(att);
            }
            return default(TValue);
        }
        
        public static TValue GetAttributeValue<TAttribute, TValue>(this MethodInfo type, Func<TAttribute, TValue> valueSelector) where TAttribute : Attribute
        {
            if (type.GetCustomAttributes(typeof(TAttribute), true).FirstOrDefault() is TAttribute att)
            {
                return valueSelector(att);
            }
            return default(TValue);
        }

        public static List<MethodInfo> GetSubCommands(this Type command)
        {
            return command.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(m => m.GetCustomAttributes(typeof(CommandAttribute), true).Length > 0).ToList();
        }
    }
}