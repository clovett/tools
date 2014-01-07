using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

//must be in the same namespace as PropertBag.
namespace OutlookSync.Model
{
    static class ReflectionExtensions
    {
        public static object[] GetEnumValues(this Type type)
        {
            List<object> result = new List<object>();
            if (type.IsEnum)
            {
                FieldInfo[] info = type.GetFields(BindingFlags.Public | BindingFlags.Static);
                if (info != null)
                {
                    foreach (FieldInfo f in info)
                    {
                        object v = f.GetValue(null);
                        result.Add(v);
                    }
                }
            }
            return result.ToArray();
        }
    }
}
