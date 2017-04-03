using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace CircleHsiao.Extensions
{
    public static class Extensions
    {
        public static void CleanTmpFile(string dir)
        {
            if (Directory.Exists(dir)) {
                string[] tmpFilePaths = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories)
                    .Where(s => s.EndsWith(".tmp") || s.EndsWith(".PendingOverwrite")).ToArray();
                foreach (var path in tmpFilePaths) {
                    DeleteFile(path);
                }
            }
        }

        /// <summary>Get random string(formed by eng uper, lower char and num)</summary>
        /// <param name="len"></param>
        /// <returns></returns>
        public static string GetRandomString(int len)
        {
            var str = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            var rand = new Random(Guid.NewGuid().GetHashCode());
            var builder = new StringBuilder();
            for (var i = 0; i < len; i++) {
                builder.Append(str[rand.Next(0, str.Length)]);
            }

            return builder.ToString();
        }

        /// <summary>Get random chinese strs</summary>
        /// <param name="len"></param>
        /// <returns></returns>
        public static string GetRandomChinese(int len)
        {
            var str = @"安八把百包報北被本比便表別並不部才產場常長車成出處此次從達
                        打大代但當到道德得的等地第點電定東動都度對多而兒二發法反方
                        分風服幹感高告個各給更工公古股關光廣國果過海好和合黑很紅後
                        華化話還回會活或機幾己濟記家加價間見件將交教接解界金進京經
                        九酒就軍開看可克口來勞老理裏利立力聯兩了林流路馬麼沒每美門
                        們面民明名目那南難內能你年女票品平期七其起企氣錢前親青情請
                        球去全卻然讓人認日如入三色山上少社身深聲生師十時什實識使世
                        事是市收手受數說斯私司四所他它她台特體天通同統頭外完萬為位
                        文問我無五物西下先現相想向象小校些謝新心信形行學樣要也業一
                        已以意因應用由有又于與元原員月再在摘者這真正政知之只至制中
                        種重主著資子自總走最做作
                        許功蓋";
            str = str.RemoveStrs(" ", Environment.NewLine);
            var rand = new Random(Guid.NewGuid().GetHashCode());
            var builder = new StringBuilder();
            for (var i = 0; i < len; i++) {
                builder.Append(str[rand.Next(0, str.Length)]);
            }

            return builder.ToString();
        }

        /// <summary>Get radom num in spec range</summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static int GetRandomNum(int from, int to)
        {
            var rand = new Random(Guid.NewGuid().GetHashCode());

            return rand.Next(from, to);
        }

        public static double GetRandomNum(double from, double to)
        {
            var rand = new Random(Guid.NewGuid().GetHashCode());

            return rand.NextDouble() * (from - to) + to;
        }

        public static DateTime GetRandomDateInRange(DateTime start, DateTime end)
        {
            var rand = new Random(Guid.NewGuid().GetHashCode());
            int range = (end - start).Days;
            return start.AddDays(rand.Next(range));
        }

        public static T GetRandomOneFromParameters<T>(params T[] args)
        {
            return args[GetRandomNum(0, args.Count())];
        }

        /// <summary>Check if path is dir(not file)</summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool CheckPathIsDir(string path)
        {
            FileAttributes attr = File.GetAttributes(path);

            return (attr & FileAttributes.Directory) == FileAttributes.Directory;
        }

        /// <summary>Move all files(include child dir) from one dir to another(will create dir if not exist)</summary>
        /// <param name="srcDir"></param>
        /// <param name="destDir"></param>
        public static void MoveAllFiles(string srcDir, string destDir)
        {
            foreach (string dirPath in Directory.GetDirectories(srcDir, "*",
                SearchOption.AllDirectories))
                Directory.CreateDirectory(dirPath.Replace(srcDir, destDir));

            foreach (string newPath in Directory.GetFiles(srcDir, "*.*",
                SearchOption.AllDirectories))
                File.Copy(newPath, newPath.Replace(srcDir, destDir), true);
        }

        /// <summary>Delete file</summary>
        /// <param name="fullPath"></param>
        public static void DeleteFile(string fullPath)
        {
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }

        #region Extension methods

        /// <summary>Extension method for lst&lt;obj&gt; to DataTable</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <returns></returns>
        public static DataTable ToDataTable<T>(this IEnumerable<T> enumerable)
        {
            PropertyDescriptorCollection props =
                TypeDescriptor.GetProperties(typeof(T));
            DataTable table = new DataTable();
            for (int i = 0; i < props.Count; i++) {
                PropertyDescriptor prop = props[i];
                Type propType = prop.PropertyType;
                // To avoid DataSet isnt support System.Nullable<> exception
                if (propType.IsGenericType &&
                    propType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    propType = Nullable.GetUnderlyingType(propType);

                table.Columns.Add(prop.Name, propType);
            }
            object[] values = new object[props.Count];
            foreach (T item in enumerable) {
                for (int i = 0; i < values.Length; i++) {
                    values[i] = props[i].GetValue(item);
                }
                table.Rows.Add(values);
            }
            return table;
        }

        /// <summary>Remove spec strs from oriStr</summary>
        /// <param name="oriStr">original string</param>
        /// <param name="strs">strings to remove</param>
        /// <returns>string after cut</returns>
        public static string RemoveStrs(this string oriStr, params string[] strs)
        {
            strs.ToList().ForEach(str => oriStr = oriStr.Replace(str, ""));
            return oriStr;
        }

        public static string ReplaceLastStr(this string src, string target, string replacement)
        {
            int index = src.LastIndexOf(target);

            if (index == -1)
                return src;

            return src.Remove(index, target.Length).Insert(index, replacement);
        }

        public static string RemoveLastStr(this string src, string target)
        {
            return ReplaceLastStr(src, target, "");
        }

        public static bool In<T>(this T source, params T[] list)
        {
            return list.Contains(source);
        }

        public static bool NotIn<T>(this T source, params T[] list)
        {
            return !list.Contains(source);
        }

        public static string RemoveAfterFirstStr(this string src, string afterThis)
        {
            int index = src.IndexOf(afterThis);
            if (index > 0)
                return src.Substring(0, index);

            return src;
        }

        /// <summary>
        /// Check if one IEnumerable contains another
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="superSet"></param>
        /// <param name="subSet"></param>
        /// <returns></returns>
        public static bool ContainAnotherWholeIEumable<T>(this IEnumerable<T> superSet, IEnumerable<T> subSet)
        {
            return !subSet.Except(superSet).Any();
        }

        /// <summary>
        /// Compare two list ignore seq
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list1"></param>
        /// <param name="list2"></param>
        /// <returns></returns>
        public static bool ElementsEquals<T>(this IEnumerable<T> list1, IEnumerable<T> list2)
        {
            var cnt = new Dictionary<T, int>();
            foreach (T s in list1) {
                if (cnt.ContainsKey(s)) {
                    cnt[s]++;
                }
                else {
                    cnt.Add(s, 1);
                }
            }
            foreach (T s in list2) {
                if (cnt.ContainsKey(s)) {
                    cnt[s]--;
                }
                else {
                    return false;
                }
            }
            return cnt.Values.All(c => c == 0);
        }

        public static IList<T> Clone<T>(this IList<T> listToClone) where T : ICloneable
        {
            return listToClone.Select(item => (T)item.Clone()).ToList();
        }

        public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> items, Func<T, TKey> property)
        {
            return items.GroupBy(property).Select(x => x.First());
        }
        #endregion
    }
}