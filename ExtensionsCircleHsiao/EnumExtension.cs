using System;
using System.ComponentModel;
using System.Linq;
using System.Web.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace CircleHsiao.idv.Extensions
{
    public static class EnumExtension
    {

        public static string GetDisplayName(this Enum val)
        {
            return val.GetType()
                            .GetMember(val.ToString())
                            .First()
                            .GetCustomAttribute<DisplayAttribute>()
                            .GetName();
        }

        public static SelectList ToSelectList<TEnum>(this TEnum enumObj)
            where TEnum : struct, IComparable, IFormattable, IConvertible
        {
            var values = from TEnum e in Enum.GetValues(typeof(TEnum))
                         select new
                         {
                             Id = e,
                             Name = typeof(TEnum).GetMember(e.ToString()).First().GetCustomAttribute<DisplayAttribute>().GetName()
                         };
            return new SelectList(values, "Id", "Name", enumObj);
        }

        public static string ToDescription(this Enum val)
        {
            string enumName = val.ToString();
            DescriptionAttribute[] attributes =
                (DescriptionAttribute[])val.GetType()
                .GetField(enumName)
                .GetCustomAttributes(typeof(DescriptionAttribute), false);

            return attributes.Length > 0 ? attributes[0].Description : enumName;
        }
    }
}
