using System.Runtime.InteropServices;
using System.Text;

namespace CircleHsiao.Extensions
{
    public class INI
    {
        private string _iniFilePath;

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string lpAppName, string lpKeyName, string lpString, string lpFileName);

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string lpAppName, string lpKeyName, string lpDefault, StringBuilder lpReturnedString, int nSize, string lpFileName);

        public INI()
        {
        }

        public INI(string fullPath)
        {
            _iniFilePath = fullPath;
        }

        public void Write(string section, string key, string val, string iniFilePath)
        {
            WritePrivateProfileString(section, key, val, iniFilePath);
        }

        public void Write(string section, string key, string val)
        {
            WritePrivateProfileString(section, key, val, _iniFilePath);
        }

        public string Read(string section, string key, string iniFilePath)
        {
            StringBuilder sb = new StringBuilder(255);
            int i = GetPrivateProfileString(section, key, string.Empty, sb, 255, iniFilePath);
            return sb.ToString();
        }

        public string Read(string section, string key)
        {
            StringBuilder sb = new StringBuilder(255);
            int i = GetPrivateProfileString(section, key, string.Empty, sb, 255, _iniFilePath);
            return sb.ToString();
        }

        public string ReadOrDefault(string section, string key, string defaultVal)
        {
            string ret = defaultVal;
            StringBuilder sb = new StringBuilder(255);
            int i = GetPrivateProfileString(section, key, string.Empty, sb, 255, _iniFilePath);

            return string.IsNullOrEmpty(sb.ToString()) ? defaultVal : sb.ToString();
        }

        public string ReadOrDefault(string section, string key, string defaultVal, string iniFilePath)
        {
            string ret = defaultVal;
            StringBuilder sb = new StringBuilder(255);
            int i = GetPrivateProfileString(section, key, string.Empty, sb, 255, iniFilePath);

            return string.IsNullOrEmpty(sb.ToString()) ? defaultVal : sb.ToString();
        }

        public void DeleteKey(string section, string key)
        {
            Write(section, key, null);
        }

        public void DeleteSection(string section)
        {
            Write(section, null, null);
        }

        public bool KeyExists(string key, string section)
        {
            return Read(key, section).Length > 0;
        }
    }
}