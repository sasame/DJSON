using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime;
using System.Linq;
using UnityEngine;

public class DJSON
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

    // デシリアライズ ==============
    static float getFloatValue(object o)
    {
        if (o is long)
        {
            return (float)(long)o;
        }
        else if (o is double)
        {
            return (float)(double)o;
        }
        throw new System.Exception("Error! getFloatValue:" + o);
    }
    static long getLongValue(object o)
    {
        if (o is long)
        {
            return (long)o;
        }
        else if (o is double)
        {
            return (long)(double)o;
        }
        throw new System.Exception("Error! getLongValue:" + o);
    }
    /// <summary>
    /// リストをデシリアライズ
    /// </summary>
    /// <param name="fieldType"></param>
    /// <param name="type"></param>
    /// <param name="value"></param>
    /// <param name="list"></param>
    static void deserializeList(Type fieldType, Type type, object value, List<object> list)
    {
        foreach (var item in list)
        {
            if (item == null)
            {
                fieldType.InvokeMember("Add", System.Reflection.BindingFlags.InvokeMethod, null, value, new object[] { null });
            }
            else if (type == typeof(string))
            {
                var inst = item as string;
                fieldType.InvokeMember("Add", System.Reflection.BindingFlags.InvokeMethod, null, value, new object[] { inst });
            }
            else if (isSupportValueType(type))
            {
                if (type == typeof(int))
                {
                    fieldType.InvokeMember("Add", System.Reflection.BindingFlags.InvokeMethod, null, value, new object[] { Convert.ToInt32(item) });
                }
                else if (fieldType == typeof(uint))
                {
                    fieldType.InvokeMember("Add", System.Reflection.BindingFlags.InvokeMethod, null, value, new object[] { Convert.ToUInt32(item) });
                }
                else if (fieldType == typeof(long))
                {
                    fieldType.InvokeMember("Add", System.Reflection.BindingFlags.InvokeMethod, null, value, new object[] { Convert.ToInt64(item) });
                }
                else if (fieldType == typeof(sbyte))
                {
                    fieldType.InvokeMember("Add", System.Reflection.BindingFlags.InvokeMethod, null, value, new object[] { Convert.ToSByte(item) });
                }
                else if (fieldType == typeof(byte))
                {
                    fieldType.InvokeMember("Add", System.Reflection.BindingFlags.InvokeMethod, null, value, new object[] { Convert.ToByte(item) });
                }
                else if (fieldType == typeof(short))
                {
                    fieldType.InvokeMember("Add", System.Reflection.BindingFlags.InvokeMethod, null, value, new object[] { Convert.ToInt16(item) });
                }
                else if (fieldType == typeof(ushort))
                {
                    fieldType.InvokeMember("Add", System.Reflection.BindingFlags.InvokeMethod, null, value, new object[] { Convert.ToUInt16(item) });
                }
                else if (fieldType == typeof(ulong))
                {
                    fieldType.InvokeMember("Add", System.Reflection.BindingFlags.InvokeMethod, null, value, new object[] { Convert.ToUInt64(item) });
                }
                else if (fieldType == typeof(float))
                {
                    fieldType.InvokeMember("Add", System.Reflection.BindingFlags.InvokeMethod, null, value, new object[] { Convert.ToSingle(item) });
                }
                else if (fieldType == typeof(double))
                {
                    fieldType.InvokeMember("Add", System.Reflection.BindingFlags.InvokeMethod, null, value, new object[] { Convert.ToDouble(item) });
                }
                else if (fieldType == typeof(decimal))
                {
                    fieldType.InvokeMember("Add", System.Reflection.BindingFlags.InvokeMethod, null, value, new object[] { Convert.ToDecimal(item) });
                }
                else if (fieldType == typeof(bool))
                {
                    fieldType.InvokeMember("Add", System.Reflection.BindingFlags.InvokeMethod, null, value, new object[] { Convert.ToBoolean(item) });
                }
            }
            else
            {
                var inst = Activator.CreateInstance(type);
                fieldType.InvokeMember("Add", System.Reflection.BindingFlags.InvokeMethod, null, value, new object[] { inst });

                if (item is Dictionary<string, object>)
                {
                    deserializeObject(type, ref inst, item as Dictionary<string, object>);
                }
            }
        }
    }
    /// <summary>
    /// ディクショナリをデシリアライズ
    /// </summary>
    /// <param name="fieldType">ディクショナリのタイプ</param>
    /// <param name="value">ディクショナリ実体</param>
    /// <param name="dic">オリジナルデータ</param>
    static void deserializeDictionary(Type fieldType, object value, Dictionary<string,object> dic)
    {
        var valueType = fieldType.GetGenericArguments()[1];
        if (valueType == typeof(string))
        {
            foreach (var pair in dic)
            {
                fieldType.InvokeMember("Add", System.Reflection.BindingFlags.InvokeMethod, null, value, new object[] { pair.Key, pair.Value });
            }
        }else if (valueType == typeof(bool))
        {
            foreach (var pair in dic)
            {
                fieldType.InvokeMember("Add", System.Reflection.BindingFlags.InvokeMethod, null, value, new object[] { pair.Key, pair.Value });
            }
        }else if (valueType.IsClass)
        {
            foreach (var pair in dic)
            {
                var inst = Activator.CreateInstance(valueType);
                deserializeObject(valueType, ref inst, pair.Value as Dictionary<string,object>);
                fieldType.InvokeMember("Add", System.Reflection.BindingFlags.InvokeMethod, null, value, new object[] { pair.Key, inst });
            }
        }else if (isStruct(valueType))
        {
            foreach (var pair in dic)
            {
                var inst = Activator.CreateInstance(valueType);
                deserializeObject(valueType, ref inst, pair.Value as Dictionary<string,object>);
                fieldType.InvokeMember("Add", System.Reflection.BindingFlags.InvokeMethod, null, value, new object[] { pair.Key, inst });
            }
        }
    }
    /// <summary>
    /// オブジェクトをデシリアライズ
    /// </summary>
    /// <param name="type"></param>
    /// <param name="value"></param>
    /// <param name="dic"></param>
    static void deserializeObject(Type type, ref object value, Dictionary<string, object> dic)
    {
        var fields = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        foreach (var f in fields)
        {
            if (dic.ContainsKey(f.Name) == false) continue;
            var o = dic[f.Name];
            if (o == null) continue;
            var fieldType = f.FieldType;
            if ((fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>)) && (o is List<object>))
            {
//                var listInst = f.GetValue(value);
                var listInst = Activator.CreateInstance(fieldType);
                var typeItem = fieldType.GetGenericArguments()[0];
                deserializeList(fieldType, typeItem, listInst, o as List<object>);
                f.SetValue(value,listInst);
            }
            else if ((fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(Dictionary<,>)) && (o is Dictionary<string,object>))
            {
                var inst = Activator.CreateInstance(fieldType);
                deserializeDictionary(fieldType, inst, o as Dictionary<string,object>);
                f.SetValue(value, inst);
            }
            else if (fieldType == typeof(string))
            {
                f.SetValue(value, o);
            }
            else if (fieldType == typeof(int))
            {
                f.SetValue(value, Convert.ToInt32(o));
            }
            else if (fieldType == typeof(uint))
            {
                f.SetValue(value, Convert.ToUInt32(o));
            }
            else if (fieldType == typeof(long))
            {
                f.SetValue(value, Convert.ToInt64(o));
            }
            else if (fieldType == typeof(sbyte))
            {
                f.SetValue(value, Convert.ToSByte(o));
            }
            else if (fieldType == typeof(byte))
            {
                f.SetValue(value, Convert.ToByte(o));
            }
            else if (fieldType == typeof(short))
            {
                f.SetValue(value, Convert.ToInt16(o));
            }
            else if (fieldType == typeof(ushort))
            {
                f.SetValue(value, Convert.ToUInt16(o));
            }
            else if (fieldType == typeof(ulong))
            {
                f.SetValue(value, Convert.ToUInt64(o));
            }
            else if (fieldType == typeof(float))
            {
                f.SetValue(value, Convert.ToSingle(o));
            }
            else if (fieldType == typeof(double))
            {
                f.SetValue(value, Convert.ToDouble(o));
            }
            else if (fieldType == typeof(decimal))
            {
                f.SetValue(value, Convert.ToDecimal(o));
            }
            else if (fieldType == typeof(bool))
            {
                f.SetValue(value, Convert.ToBoolean(o));
            }
            else if (fieldType.IsClass)
            {
                if (fieldType.IsArray)
                {
                    var listType = Type.GetType("System.Collections.Generic.List`1[[" + fieldType.GetElementType().Name + ", Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]]");
                    var listInst = Activator.CreateInstance(listType);
                    deserializeList(listType, fieldType.GetElementType(), listInst, o as List<object>);
                    var inst = listType.InvokeMember("ToArray", System.Reflection.BindingFlags.InvokeMethod, null,listInst,null);
                    f.SetValue(value, inst);
                }
                else
                {
                    var inst = Activator.CreateInstance(fieldType);
                    deserializeObject(fieldType, ref inst, o as Dictionary<string, object>);
                    f.SetValue(value, inst);
                }
            }
            else if (isStruct(fieldType))
            {
                var inst = f.GetValue(value);
                deserializeObject(fieldType, ref inst, o as Dictionary<string, object>);
                f.SetValue(value, inst);
            }
            else
            {
                Debug.Log("undefined type:" + fieldType);
            }
        }
    }
    public static T Deserialize<T>(string jsonString)
    {
        try
        {
            var o = Parse(jsonString);
//            Debug.Log(jsonString);
            var type = typeof(T);
            if ((type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)) && (o is List<object>))
            {
                // デバッグ出来ていない
                var list = o as List<object>;
                var value = (T)Activator.CreateInstance(type);
                deserializeList(type, type, value, list);
                return value;
            }
            else if ((type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>)) && (o is Dictionary<string, object>))
            {
                var inst = Activator.CreateInstance(type);
                deserializeDictionary(type, inst, o as Dictionary<string, object>);
                return (T)inst;
            }
            else
            {
                var value = (T)Activator.CreateInstance(type);
                var inst = (object)value;
                deserializeObject(type, ref inst, o as Dictionary<string, object>);
                if (isStruct(type)) value = (T)inst;
                return value;
            }
        }catch(Exception ex)
        {
            Debug.Log(ex.Message);
            return default(T);
        }
    }


    static Dictionary<char, string> convertTable = null;
    static void MakeConvertTable()
    {
        if (convertTable == null)
        {
            convertTable = new Dictionary<char, string>();
            convertTable['"'] = "\\\"";
            convertTable['\\'] = "\\\\";
            convertTable['\b'] = "\\b";
            convertTable['\f'] = "\\f";
            convertTable['\n'] = "\\n";
            convertTable['\r'] = "\\r";
            convertTable['\t'] = "\\t";
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
            if (convertTable.ContainsKey(c))
            {
                b.Append(convertTable[c]);
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
        foreach (var t in _supportValueTypes)
        {
            if (t == type) return true;
        }
        return false;
    }

    static Dictionary<string, object> serializeObject(Type type, object value)
    {
        if (value == null) return null;

        Dictionary<string, object> dic = new Dictionary<string, object>();
        // ディクショナリ
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            Type keyType = type.GetGenericArguments()[0];
            Type valueType = type.GetGenericArguments()[1];
            if (keyType == typeof(string))
            {
                var items = type.GetProperty("Keys", BindingFlags.Instance | BindingFlags.Public).GetValue(value) as IEnumerable;
                var values = type.GetProperty("Values", BindingFlags.Instance | BindingFlags.Public).GetValue(value) as ICollection;
                object[] keys = items.OfType<object>().ToArray();
                int indexKey = 0;
                foreach(var v in values)
                {
                    var key = keys[indexKey] as string;
                    if (isSupportValueType(valueType))
                    {
                        dic[key] = v;
                    }
                    else if (valueType.IsClass)
                    {
                        dic[key] = serializeObject(valueType, v);
                    }
                    else if (isStruct(valueType))
                    {
                        dic[key] = serializeObject(valueType, v);
                    }
                    else
                    {
                        UnityEngine.Debug.Log("ignore type:" + valueType);
                    }
                    ++indexKey;
                }
            }
        }
        else
        {
            var fields = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            foreach (var f in fields)
            {
                var fieldValue = f.GetValue(value);
                var fieldType = f.FieldType;
                if (fieldValue == null)
                {
                    dic[f.Name] = fieldValue;
                }
                else if (isSupportValueType(fieldType))
                {
                    dic[f.Name] = fieldValue;
                }
                else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    // list
                    var list = new List<object>();
                    var fieldList = fieldValue as IList;
                    foreach (var item in fieldList)
                    {
                        if (item.GetType() == typeof(string))
                        {
                            list.Add(item);
                        }
                        else if (isSupportValueType(item.GetType()))
                        {
                            list.Add(item);
                        }
                        else if (isStruct(fieldType))
                        {
                            dic[f.Name] = serializeObject(fieldType, fieldValue);
                        }
                        else if (item.GetType().IsClass)
                        {
                            list.Add(serializeObject(item.GetType(), item));
                        }
                    }
                    dic[f.Name] = list;
                }
                else if (fieldType.IsArray)
                {
                    // array
                    var list = new List<object>();
                    var fieldList = fieldValue as Array;
                    foreach (var item in fieldList)
                    {
                        if (item != null)
                        {
                            var arrayItem = serializeObject(item.GetType(), item);
                            list.Add(arrayItem);
                        }
                        else
                        {
                            list.Add(null);
                        }
                    }
                    dic[f.Name] = list;
                }
                else if (fieldType.IsClass)
                {
                    dic[f.Name] = serializeObject(fieldType, fieldValue);
                }
                else if (isStruct(fieldType))
                {
                    dic[f.Name] = serializeObject(fieldType, fieldValue);
                }
                else
                {
                    UnityEngine.Debug.Log("ignore type:" + fieldType);
                }
            }
        }
        return dic;
    }

    public static string Serialize<T>(T value)
    {
        Type type = typeof(T);

        // objectタイプ
        var result = serializeObject(type, value);
        return ToJson(result);
    }
}
