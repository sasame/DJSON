using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

/// <summary>
/// JSON変換
/// </summary>
public partial class DJSON
{
    static bool isStruct(Type t)
    {
        return t.IsValueType && (!t.IsPrimitive) && (!t.IsEnum);
    }

    public static object Parse(string jsonString)
    {
        try
        {
            object o = ParseInner(jsonString);
            return o;
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.Log(ex.Message);
        }
        return null;
    }

    const string _sign = "{}[],:\"";
    static bool IsSign(char c)
    {
        return char.IsWhiteSpace(c) || (_sign.IndexOf(c) != -1);
    }

    static object ParseInner(string jsonString)
    {
        List<object> curObj = new List<object>();
        int len = jsonString.Length;
        string tmpName = null;
        object firstObj = null;
        for (int cur = 0; cur < len; ++cur)
        {
            char curChar = jsonString[cur];
            object lastObj = null;
            if (curObj.Count > 0)
            {
                lastObj = curObj[curObj.Count - 1];
            }
            if (char.IsWhiteSpace(curChar))
            {
                continue;
            }
            switch (curChar)
            {
                case '{':
                    // 連想配列開始
                    {
                        Dictionary<string, object> dic = new Dictionary<string, object>();
                        AddObject(curObj, ref tmpName, dic);
                        curObj.Add(dic);
                    }
                    break;
                case '}':
                    // 連想配列終了
                    {
                        if (lastObj is Dictionary<string, object>)
                        {
                            curObj.RemoveAt(curObj.Count - 1);
                        }
                        else
                        {
                            UnityEngine.Debug.Log("ERROR } ");
                        }
                    }
                    break;
                case '[':
                    // 配列開始
                    {
                        List<object> list = new List<object>();
                        AddObject(curObj, ref tmpName, list);
                        curObj.Add(list);
                    }
                    break;
                case ']':
                    // 配列終了
                    {
                        // curObj.last == List<object>という条件が必要
                        if (lastObj is List<object>)
                        {
                            curObj.RemoveAt(curObj.Count - 1);
                        }
                        else
                        {
                            UnityEngine.Debug.Log("ERROR ] ");
                        }
                    }
                    break;
                case ',':
                    break;
                case '"':
                    // 文字列
                    {
                        //                            UnityEngine.Debug.Log("STRING START:"+ cur);
                        ++cur;

                        // 文字列初めから文字を読み取っていく
                        StringBuilder builder = new StringBuilder();
                        do
                        {
                            if (jsonString[cur] == '"')
                            {
                                break;
                            }
                            else if (jsonString[cur] == '\\')
                            {
                                // エスケープ文字を処理する
                                char nextChar = jsonString[cur + 1];
                                switch (nextChar)
                                {
                                    case '"':
                                    case '\\':
                                    case '/':
                                        builder.Append(nextChar);
                                        break;
                                    case 'b':
                                        builder.Append('\b');
                                        break;
                                    case 'f':
                                        builder.Append('\f');
                                        break;
                                    case 'n':
                                        builder.Append('\n');
                                        break;
                                    case 'r':
                                        builder.Append('\r');
                                        break;
                                    case 't':
                                        builder.Append('\t');
                                        break;
                                    case 'u':
                                        if (cur + 5 < len)
                                        {
                                            char[] fourChar = new char[] { jsonString[cur + 2], jsonString[cur + 3], jsonString[cur + 4], jsonString[cur + 5] };
                                            builder.Append((char)Convert.ToInt32(new string(fourChar), 16));
                                            cur += 4;
                                        }
                                        break;
                                }
                                cur += 2;
                            }
                            else
                            {
                                builder.Append(jsonString[cur]);
                                ++cur;
                            }
                        } while (cur < len);

                        // string はどこのものか
                        AddObject(curObj, ref tmpName, builder.ToString());
                    }
                    break;
                case ':':
                    break;
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                case '-':
                    {
                        // 数値
                        int start = cur;
                        bool hasPoint = false;
                        for (; cur < len; ++cur)
                        {
                            char c = jsonString[cur];
                            if (IsSign(c))
                            {
                                break;
                            }
                            else if (c == '.')
                            {
                                hasPoint = true;
                            }
                        }
                        string numStr = jsonString.Substring(start, cur - start);

                        object val = null;
                        if (hasPoint)
                        {
                            double d;
                            if (double.TryParse(numStr, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out d))
                            {
                                val = d;
                            }
                        }
                        else
                        {
                            long v;
                            if (long.TryParse(numStr, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out v))
                            {
                                val = v;
                            }
                        }

                        AddObject(curObj, ref tmpName, val);
                        --cur;
                    }
                    break;
                default:
                    {
                        // その他の文字列
                        int start = cur;
                        for (; cur < len; ++cur)
                        {
                            char c = jsonString[cur];
                            if (IsSign(c))
                            {
                                //                                    --cur;
                                break;
                            }
                        }
                        string str = jsonString.Substring(start, cur - start);
                        object val = null;
                        if (str == "true")
                        {
                            val = true;
                        }
                        else if (str == "false")
                        {
                            val = false;
                        }
                        else if (str == "null")
                        {
                            val = null;
                        }
                        else
                        {
                            val = str;
                        }

                        AddObject(curObj, ref tmpName, val);
                        --cur;
                    }
                    break;
            }

            // 最初のオブジェクトを返り値にする
            if (firstObj == null)
            {
                if (curObj.Count > 0)
                {
                    firstObj = curObj[0];
                }
            }
        }
        return firstObj;
    }

    static void AddObject(List<object> list, ref string tmpName, object val)
    {
        object lastObj = null;
        if (list.Count > 0)
        {
            lastObj = list[list.Count - 1];
        }
        if (lastObj == null)
        {
            // 単純に値
            list.Add(val);
        }
        else if (tmpName == null)
        {
            if (lastObj is List<object>)
            {
                // 配列に追加
                (lastObj as List<object>).Add(val);
            }
            else if (lastObj is Dictionary<string, object>)
            {
                if (val is string)
                {
                    tmpName = (val as string);
                }
            }
        }
        else
        {
            if (lastObj is Dictionary<string, object>)
            {
                // 連想配列に追加
                (lastObj as Dictionary<string, object>).Add(tmpName, val);
                tmpName = null;
            }
        }
    }

    static Dictionary<char, string> _convertTable = null;
    static void MakeConvertTable()
    {
        if (_convertTable == null)
        {
            _convertTable = new Dictionary<char, string>();
            _convertTable['"'] = "\\\"";
            _convertTable['\\'] = "\\\\";
            _convertTable['\b'] = "\\b";
            _convertTable['\f'] = "\\f";
            _convertTable['\n'] = "\\n";
            _convertTable['\r'] = "\\r";
            _convertTable['\t'] = "\\t";
        }
    }
    public static string ToJson(object o, bool formated = false)
    {
        MakeConvertTable();
        StringBuilder builder = new StringBuilder();
        AppendValue(builder, o, "", formated);
        return builder.ToString();
    }

    static void AppendValue(StringBuilder b, object o, string tab, bool formated)
    {
        if (o == null)
        {
            b.Append("null");
        }
        else if (o is string)
        {
            AppendString(b, o as string);
        }
        else if (o is bool)
        {
            b.Append((bool)o ? "true" : "false");
        }
        else if (o is IList)
        {
            AppendList(b, o as IList, tab, formated);
        }
        else if (o is IDictionary)
        {
            AppendDictionary(b, o as IDictionary, tab, formated);
        }
        else if (o is float)
        {
            b.Append(((float)o).ToString("R"));
        }
        else if (o is int
              || o is uint
              || o is long
              || o is sbyte
              || o is byte
              || o is short
              || o is ushort
              || o is ulong)
        {
            b.Append(o);
        }
        else if (o is double
          || o is decimal)
        {
            b.Append(Convert.ToDouble(o).ToString("R"));
        }
        else
        {
            if (o == null)
            {
                UnityEngine.Debug.Log("o is null");
            }
            if (o is string)
            {
                AppendString(b, o as string);
            }
            else
            {
                UnityEngine.Debug.Log("o is not string:" + o);
            }
        }
    }
    static void AppendString(StringBuilder b, string s)
    {
        Encoding enc = Encoding.UTF8;
        b.Append('\"');
        foreach (char c in s)
        {
            if (_convertTable.ContainsKey(c))
            {
                b.Append(_convertTable[c]);
            }
            else
            {
                int charCode = Convert.ToInt32(c);
                if ((charCode >= 32) && (charCode <= 126))
                {
                    b.Append(c);
                }
                else
                {
                    b.Append("\\u");
                    b.Append(charCode.ToString("x4"));
                }
            }
        }
        b.Append('\"');
    }
    static void AppendList(StringBuilder b, IList list, string tab, bool formated)
    {
        if (formated)
        {
            b.Append(tab);
        }
        b.Append('[');
        bool isFirst = true;
        foreach (object o in list)
        {
            if (isFirst)
            {
                isFirst = false;
            }
            else
            {
                b.Append(',');
            }
            AppendValue(b, o, tab + "  ", formated);
        }
        b.Append(']');
        if (formated)
        {
            b.Append(System.Environment.NewLine);
        }
    }
    static void AppendDictionary(StringBuilder b, IDictionary dic, string tab, bool formated)
    {
        if (formated)
        {
            b.Append(tab);
        }
        b.Append('{');
        bool isFirst = true;
        foreach (object o in dic.Keys)
        {
            if (isFirst)
            {
                isFirst = false;
            }
            else
            {
                b.Append(',');
            }
            AppendValue(b, o, tab, formated);
            b.Append(':');
            AppendValue(b, dic[o], tab, formated);
        }
        b.Append('}');
        if (formated)
        {
            b.Append(System.Environment.NewLine);
        }
    }

    // シリアライズ処理 =====
    static Type[] _supportValueTypes = new Type[]
    {
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(sbyte),
            typeof(byte),
            typeof(short),
            typeof(ushort),
            typeof(ulong),
            typeof(float),
            typeof(double),
            typeof(decimal),
            typeof(bool),
            typeof(string),
    };
    static bool isSupportValueType(Type type)
    {
        return _supportValueTypes.Contains(type);
/*        foreach (var t in _supportValueTypes)
        {
            if (t == type) return true;
        }
        return false;*/
    }


}
