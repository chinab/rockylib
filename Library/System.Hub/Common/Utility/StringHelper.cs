using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics.Contracts;
using System.Text.RegularExpressions;

namespace System
{
    public static class StringHelper
    {
        #region Properties
        public static string NowDateString
        {
            get { return DateTime.Now.ToString("yyyyMMddHHmmssfff"); }
        }
        #endregion

        #region Methods
        public static string ByteSubstring(string input, int len, string extra = null)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            int tempLen = 0;
            var sb = new StringBuilder();
            byte[] bytes = Encoding.ASCII.GetBytes(input);
            for (int i = 0; i < bytes.Length; i++)
            {
                if (i >= input.Length)
                {
                    break;
                }
                tempLen += ((int)bytes[i] == 63) ? 2 : 1;
                sb.Append(input.Substring(i, 1));
                if (tempLen > len)
                {
                    break;
                }
            }
            if (extra != null && bytes.Length > len)
            {
                sb.Append(extra);
            }
            return sb.ToString();
        }

        /// <summary>
        /// 阿拉伯数字的金额转换为中文大写数字
        /// format("{0,14:N2}:{1}") = 751,181.17:柒拾伍萬壹仟壹佰捌拾壹元壹角柒分
        /// </summary>
        /// <param name="rmb"></param>
        /// <returns></returns>
        public static string ConvertToRMB(double rmb)
        {
            string s = rmb.ToString("#L#E#D#C#K#E#D#C#J#E#D#C#I#E#D#C#H#E#D#C#G#E#D#C#F#E#D#C#.0B0A");
            string d = Regex.Replace(s, @"((?<=-|^)[^1-9]*)|((?'z'0)[0A-E]*((?=[1-9])|(?'-z'(?=[F-L\.]|$))))|((?'b'[F-L])(?'z'0)[0A-L]*((?=[1-9])|(?'-z'(?=[\.]|$))))", "${b}${z}");
            return Regex.Replace(d, ".", delegate(Match m)
            {
                return "负元空零壹贰叁肆伍陆柒捌玖空空空空空空空分角拾佰仟萬億兆京垓秭穰"[m.Value[0] - '-'].ToString();
            });
        }
        #endregion

        #region Nested
        public struct ValidationRegularExp
        {
            public const string Chinese = @"^[\u4E00-\u9FA5\uF900-\uFA2D]+$";
            public const string Color = "^#[a-fA-F0-9]{6}";
            public const string Date = @"^((((1[6-9]|[2-9]\d)\d{2})-(0?[13578]|1[02])-(0?[1-9]|[12]\d|3[01]))|(((1[6-9]|[2-9]\d)\d{2})-(0?[13456789]|1[012])-(0?[1-9]|[12]\d|30))|(((1[6-9]|[2-9]\d)\d{2})-0?2-(0?[1-9]|1\d|2[0-8]))|(((1[6-9]|[2-9]\d)(0[48]|[2468][048]|[13579][26])|((16|[2468][048]|[3579][26])00))-0?2-29-))$";
            public const string DateTime = @"^((((1[6-9]|[2-9]\d)\d{2})-(0?[13578]|1[02])-(0?[1-9]|[12]\d|3[01]))|(((1[6-9]|[2-9]\d)\d{2})-(0?[13456789]|1[012])-(0?[1-9]|[12]\d|30))|(((1[6-9]|[2-9]\d)\d{2})-0?2-(0?[1-9]|1\d|2[0-8]))|(((1[6-9]|[2-9]\d)(0[48]|[2468][048]|[13579][26])|((16|[2468][048]|[3579][26])00))-0?2-29-)) (20|21|22|23|[0-1]?\d):[0-5]?\d:[0-5]?\d$";
            public const string Email = @"^[\w-]+(\.[\w-]+)*@[\w-]+(\.[\w-]+)+$";
            public const string Float = @"^(-?\d+)(\.\d+)?$";
            public const string ImageFormat = @"\.(?i:jpg|bmp|gif|ico|pcx|jpeg|tif|png|raw|tga)$";
            public const string Integer = @"^-?\d+$";
            public const string IP = @"^(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])$";
            public const string Letter = "^[A-Za-z]+$";
            public const string LowerLetter = "^[a-z]+$";
            public const string MinusFloat = @"^(-(([0-9]+\.[0-9]*[1-9][0-9]*)|([0-9]*[1-9][0-9]*\.[0-9]+)|([0-9]*[1-9][0-9]*)))$";
            public const string MinusInteger = "^-[0-9]*[1-9][0-9]*$";
            /// <summary>
            /// "^0{0,1}1[358]\d{9}$"
            /// </summary>
            public const string Mobile = "^0{0,1}((13[0-9]{9})|(15[0-9]{9})|(18[6-9]{1}[0-9]{8}))$";
            public const string NumbericOrLetterOrChinese = @"^[A-Za-z0-9\u4E00-\u9FA5\uF900-\uFA2D]+$";
            public const string Numeric = "^[0-9]+$";
            public const string NumericOrLetter = "^[A-Za-z0-9]+$";
            public const string NumericOrLetterOrUnderline = @"^\w+$";
            public const string PlusFloat = @"^(([0-9]+\.[0-9]*[1-9][0-9]*)|([0-9]*[1-9][0-9]*\.[0-9]+)|([0-9]*[1-9][0-9]*))$";
            public const string PlusInteger = "^[0-9]*[1-9][0-9]*$";
            public const string PostCode = @"^\d{6}$";
            public const string Telephone = @"(\d+-)?(\d{4}-?\d{7}|\d{3}-?\d{8}|^\d{7,8})(-\d+)?";
            public const string UnMinusFloat = @"^\d+(\.\d+)?$";
            public const string UnMinusInteger = @"^\d+$";
            public const string UnPlusFloat = @"^((-\d+(\.\d+)?)|(0+(\.0+)?))$";
            public const string UnPlusInteger = @"^((-\d+)|(0+))$";
            public const string UpperLetter = "^[A-Z]+$";
            public const string Url = @"^http(s)?://([\w-]+\.)+[\w-]+(/[\w- ./?%&=]*)?$";
        }
        #endregion

        #region Validation
        /// <summary>
        /// 验证非负整数（正整数 + 0）
        /// </summary>
        /// <param name="input">要验证的字符串</param>
        /// <returns>验证通过返回true</returns>
        public static bool IsUnMinusInt(string input)
        {
            return input == null ? false : Regex.IsMatch(input, ValidationRegularExp.UnMinusInteger);
        }

        /// <summary>
        /// 验证正整数
        /// </summary>
        /// <param name="input">要验证的字符串</param>
        /// <returns>验证通过返回true</returns>
        public static bool IsPlusInt(string input)
        {
            return input == null ? false : Regex.IsMatch(input, ValidationRegularExp.PlusInteger);
        }

        /// <summary>
        /// 验证非正整数（负整数 + 0） 
        /// </summary>
        /// <param name="input">要验证的字符串</param>
        /// <returns>验证通过返回true</returns>
        public static bool IsUnPlusInt(string input)
        {
            return input == null ? false : Regex.IsMatch(input, ValidationRegularExp.UnPlusInteger);
        }

        /// <summary>
        /// 检测是否符合邮编格式
        /// </summary>
        /// <param name="postCode"></param>
        /// <returns></returns>
        public static bool IsPostCode(string input)
        {
            return input == null ? false : Regex.IsMatch(input, ValidationRegularExp.PostCode);
        }

        /// <summary>
        /// 验证负整数
        /// </summary>
        /// <param name="input">要验证的字符串</param>
        /// <returns>验证通过返回true</returns>
        public static bool IsMinusInt(string input)
        {
            return input == null ? false : Regex.IsMatch(input, ValidationRegularExp.MinusInteger);
        }

        /// <summary>
        /// 验证整数
        /// </summary>
        /// <param name="input">要验证的字符串</param>
        /// <returns>验证通过返回true</returns>
        public static bool IsInt(string input)
        {
            return input == null ? false : Regex.IsMatch(input, ValidationRegularExp.Integer);
        }

        /// <summary>
        /// 验证非负浮点数（正浮点数 + 0）
        /// </summary>
        /// <param name="input">要验证的字符串</param>
        /// <returns>验证通过返回true</returns>
        public static bool IsUnMinusFloat(string input)
        {
            return input == null ? false : Regex.IsMatch(input, ValidationRegularExp.UnMinusFloat);
        }

        /// <summary>
        /// 验证正浮点数
        /// </summary>
        /// <param name="input">要验证的字符串</param>
        /// <returns>验证通过返回true</returns>
        public static bool IsPlusFloat(string input)
        {
            return input == null ? false : Regex.IsMatch(input, ValidationRegularExp.PlusFloat);
        }

        /// <summary>
        /// 验证非正浮点数（负浮点数 + 0）
        /// </summary>
        /// <param name="input">要验证的字符串</param>
        /// <returns>验证通过返回true</returns>
        public static bool IsUnPlusFloat(string input)
        {
            return input == null ? false : Regex.IsMatch(input, ValidationRegularExp.UnPlusFloat);
        }

        /// <summary>
        /// 验证负浮点数
        /// </summary>
        /// <param name="input">要验证的字符串</param>
        /// <returns>验证通过返回true</returns>
        public static bool IsMinusFloat(string input)
        {
            return input == null ? false : Regex.IsMatch(input, ValidationRegularExp.MinusFloat);
        }

        /// <summary>
        /// 验证浮点数
        /// </summary>
        /// <param name="input">要验证的字符串</param>
        /// <returns>验证通过返回true</returns>
        public static bool IsFloat(string input)
        {
            return input == null ? false : Regex.IsMatch(input, ValidationRegularExp.Float);
        }

        /// <summary>
        /// 验证由26个英文字母组成的字符串
        /// </summary>
        /// <param name="input">要验证的字符串</param>
        /// <returns>验证通过返回true</returns>
        public static bool IsLetter(string input)
        {
            return input == null ? false : Regex.IsMatch(input, ValidationRegularExp.Letter);
        }

        /// <summary>
        /// 验证由中文组成的字符串
        /// </summary>
        /// <param name="input">要验证的字符串</param>
        /// <returns>验证通过返回true</returns>
        public static bool IsChinese(string input)
        {
            return input == null ? false : Regex.IsMatch(input, ValidationRegularExp.Chinese);
        }

        /// <summary>
        /// 验证由26个英文字母的大写组成的字符串
        /// </summary>
        /// <param name="input">要验证的字符串</param>
        /// <returns>验证通过返回true</returns>
        public static bool IsUpperLetter(string input)
        {
            return input == null ? false : Regex.IsMatch(input, ValidationRegularExp.UpperLetter);
        }

        /// <summary>
        /// 验证由26个英文字母的小写组成的字符串
        /// </summary>
        /// <param name="input">要验证的字符串</param>
        /// <returns>验证通过返回true</returns>
        public static bool IsLowerLetter(string input)
        {
            return input == null ? false : Regex.IsMatch(input, ValidationRegularExp.LowerLetter);
        }

        /// <summary>
        /// 验证由数字和26个英文字母组成的字符串
        /// </summary>
        /// <param name="input">要验证的字符串</param>
        /// <returns>验证通过返回true</returns>
        public static bool IsNumericOrLetter(string input)
        {
            return input == null ? false : Regex.IsMatch(input, ValidationRegularExp.NumericOrLetter);
        }

        /// <summary>
        /// 验证由数字组成的字符串
        /// </summary>
        /// <param name="input">要验证的字符串</param>
        /// <returns>验证通过返回true</returns>
        public static bool IsNumeric(string input)
        {
            return input == null ? false : Regex.IsMatch(input, ValidationRegularExp.Numeric);
        }

        /// <summary>
        /// 验证由数字和26个英文字母或中文组成的字符串
        /// </summary>
        /// <param name="input">要验证的字符串</param>
        /// <returns>验证通过返回true</returns>
        public static bool IsNumericOrLetterOrChinese(string input)
        {
            return input == null ? false : Regex.IsMatch(input, ValidationRegularExp.NumbericOrLetterOrChinese);
        }

        /// <summary>
        /// 验证由数字、26个英文字母或者下划线组成的字符串
        /// </summary>
        /// <param name="input">要验证的字符串</param>
        /// <returns>验证通过返回true</returns>
        public static bool IsNumericOrLetterOrUnderline(string input)
        {
            return input == null ? false : Regex.IsMatch(input, ValidationRegularExp.NumericOrLetterOrUnderline);
        }

        /// <summary>
        /// 验证email地址
        /// </summary>
        /// <param name="input">要验证的字符串</param>
        /// <returns>验证通过返回true</returns>
        public static bool IsEmail(string input)
        {
            return input == null ? false : Regex.IsMatch(input, ValidationRegularExp.Email);
        }

        /// <summary>
        /// 验证URL
        /// </summary>
        /// <param name="input">要验证的字符串</param>
        /// <returns>验证通过返回true</returns>
        public static bool IsUrl(string input)
        {
            return input == null ? false : Regex.IsMatch(input, ValidationRegularExp.Url);
        }

        /// <summary>
        /// 验证电话号码
        /// </summary>
        /// <param name="input">要验证的字符串</param>
        /// <returns>验证通过返回true</returns>
        public static bool IsTelephone(string input)
        {
            return input == null ? false : Regex.IsMatch(input, ValidationRegularExp.Telephone);
        }

        /// <summary>
        /// 验证手机号码
        /// </summary>
        /// <param name="input">要验证的字符串</param>
        /// <returns>验证通过返回true</returns>
        public static bool IsMobile(string input)
        {
            return input == null ? false : Regex.IsMatch(input, ValidationRegularExp.Mobile);
        }

        /// <summary>
        /// 通过文件扩展名验证图像格式
        /// </summary>
        /// <param name="input">要验证的字符串</param>
        /// <returns>验证通过返回true</returns>
        public static bool IsImageFormat(string input)
        {
            return input == null ? false : Regex.IsMatch(input, ValidationRegularExp.ImageFormat);
        }

        /// <summary>
        /// 验证IP
        /// </summary>
        /// <param name="input">要验证的字符串</param>
        /// <returns>验证通过返回true</returns>
        public static bool IsIP(string input)
        {
            return input == null ? false : Regex.IsMatch(input, ValidationRegularExp.IP);
        }

        /// <summary>
        /// 验证日期（YYYY-MM-DD）
        /// </summary>
        /// <param name="input">要验证的字符串</param>
        /// <returns>验证通过返回true</returns>
        public static bool IsDate(string input)
        {
            return input == null ? false : Regex.IsMatch(input, ValidationRegularExp.Date);
        }

        /// <summary>
        /// 验证日期和时间（YYYY-MM-DD HH:MM:SS）
        /// </summary>
        /// <param name="input">要验证的字符串</param>
        /// <returns>验证通过返回true</returns>
        public static bool IsDateTime(string input)
        {
            return input == null ? false : Regex.IsMatch(input, ValidationRegularExp.DateTime);
        }

        /// <summary>
        /// 验证颜色（#ff0000）
        /// </summary>
        /// <param name="input">要验证的字符串</param>
        /// <returns>验证通过返回true</returns>
        public static bool IsColor(string input)
        {
            return input == null ? false : Regex.IsMatch(input, ValidationRegularExp.Color);
        }
        #endregion
    }
}