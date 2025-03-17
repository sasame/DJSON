using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;

/// <summary>
/// JSON変換　デシリアライズ処理
/// </summary>
public partial class DJSON
{
    // デシリアライズ ==============
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
                else if (type == typeof(uint))
                {
                    fieldType.InvokeMember("Add", System.Reflection.BindingFlags.InvokeMethod, null, value, new object[] { Convert.ToUInt32(item) });
                }
                else if (type == typeof(long))
                {
                    fieldType.InvokeMember("Add", System.Reflection.BindingFlags.InvokeMethod, null, value, new object[] { Convert.ToInt64(item) });
                }
                else if (type == typeof(sbyte))
                {
                    fieldType.InvokeMember("Add", System.Reflection.BindingFlags.InvokeMethod, null, value, new object[] { Convert.ToSByte(item) });
                }
                else if (type == typeof(byte))
                {
                    fieldType.InvokeMember("Add", System.Reflection.BindingFlags.InvokeMethod, null, value, new object[] { Convert.ToByte(item) });
                }
                else if (type == typeof(short))
                {
                    fieldType.InvokeMember("Add", System.Reflection.BindingFlags.InvokeMethod, null, value, new object[] { Convert.ToInt16(item) });
                }
                else if (type == typeof(ushort))
                {
                    fieldType.InvokeMember("Add", System.Reflection.BindingFlags.InvokeMethod, null, value, new object[] { Convert.ToUInt16(item) });
                }
                else if (type == typeof(ulong))
                {
                    fieldType.InvokeMember("Add", System.Reflection.BindingFlags.InvokeMethod, null, value, new object[] { Convert.ToUInt64(item) });
                }
                else if (type == typeof(float))
                {
                    fieldType.InvokeMember("Add", System.Reflection.BindingFlags.InvokeMethod, null, value, new object[] { Convert.ToSingle(item) });
                }
                else if (type == typeof(double))
                {
                    fieldType.InvokeMember("Add", System.Reflection.BindingFlags.InvokeMethod, null, value, new object[] { Convert.ToDouble(item) });
                }
                else if (type == typeof(decimal))
                {
                    fieldType.InvokeMember("Add", System.Reflection.BindingFlags.InvokeMethod, null, value, new object[] { Convert.ToDecimal(item) });
                }
                else if (type == typeof(bool))
                {
                    fieldType.InvokeMember("Add", System.Reflection.BindingFlags.InvokeMethod, null, value, new object[] { Convert.ToBoolean(item) });
                }
            }
            else
            {
                var inst = Activator.CreateInstance(type);
                if (item is Dictionary<string, object>)
                {
                    deserializeObject(type, ref inst, item as Dictionary<string, object>);
                }
                fieldType.InvokeMember("Add", System.Reflection.BindingFlags.InvokeMethod, null, value, new object[] { inst });
            }
        }
    }
    /// <summary>
    /// ディクショナリをデシリアライズ
    /// </summary>
    /// <param name="fieldType">ディクショナリのタイプ</param>
    /// <param name="value">ディクショナリ実体</param>
    /// <param name="dic">オリジナルデータ</param>
    static void deserializeDictionary(Type fieldType, object value, Dictionary<string, object> dic)
    {
        var valueType = fieldType.GetGenericArguments()[1];
        if (valueType == typeof(string))
        {
            foreach (var pair in dic)
            {
                fieldType.InvokeMember("Add", System.Reflection.BindingFlags.InvokeMethod, null, value, new object[] { pair.Key, pair.Value });
            }
        }
        else if (valueType == typeof(bool))
        {
            foreach (var pair in dic)
            {
                fieldType.InvokeMember("Add", System.Reflection.BindingFlags.InvokeMethod, null, value, new object[] { pair.Key, pair.Value });
            }
        }
        else if (valueType.IsClass)
        {
            foreach (var pair in dic)
            {
                var inst = Activator.CreateInstance(valueType);
                deserializeObject(valueType, ref inst, pair.Value as Dictionary<string, object>);
                fieldType.InvokeMember("Add", System.Reflection.BindingFlags.InvokeMethod, null, value, new object[] { pair.Key, inst });
            }
        }
        else if (isStruct(valueType))
        {
            foreach (var pair in dic)
            {
                var inst = Activator.CreateInstance(valueType);
                deserializeObject(valueType, ref inst, pair.Value as Dictionary<string, object>);
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
            if (isSupportUnitySpecialType(fieldType))
            {
                convertDeserialize(f, value, o as Dictionary<string, object>);
/*                if (fieldType == typeof(Gradient))
                {
                    var sub = new subGradient();
                    object subO = sub;
                    deserializeObject(typeof(subGradient),ref subO, o as Dictionary<string, object>);
                    f.SetValue(value, new Gradient()
                    {
                        colorKeys = sub.colorKeys,
                        alphaKeys = sub.alphaKeys,
                        colorSpace = sub.colorSpace,
                        mode = sub.mode,
                    });
                }else if (fieldType == typeof(AnimationCurve))
                {
                    var sub = new subAnimationCurve();
                    object subO = sub;
                    deserializeObject(typeof(subAnimationCurve), ref subO, o as Dictionary<string, object>);
                    var vkeys = new Keyframe[sub.keys.Length];
                    for (int i = 0; i < sub.keys.Length; ++i) vkeys[i] = sub.keys[i].ToKeyframe();
                    f.SetValue(value, new AnimationCurve()
                    {
                        keys = vkeys,
                        preWrapMode = sub.preWrapMode,
                        postWrapMode = sub.postWrapMode,
                    });
                }*/
            }
            else if ((fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>)) && (o is List<object>))
            {
                //                var listInst = f.GetValue(value);
                var listInst = Activator.CreateInstance(fieldType);
                var typeItem = fieldType.GetGenericArguments()[0];
                deserializeList(fieldType, typeItem, listInst, o as List<object>);
                f.SetValue(value, listInst);
            }
            else if ((fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(Dictionary<,>)) && (o is Dictionary<string, object>))
            {
                var inst = Activator.CreateInstance(fieldType);
                deserializeDictionary(fieldType, inst, o as Dictionary<string, object>);
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
                    //                    var listType = Type.GetType("System.Collections.Generic.List`1[[" + fieldType.GetElementType().Name + ", Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]]");
                    var listType = Type.GetType("System.Collections.Generic.List`1[[" + fieldType.GetElementType().AssemblyQualifiedName + "]]");
                    var listInst = Activator.CreateInstance(listType);
                    deserializeList(listType, fieldType.GetElementType(), listInst, o as List<object>);
                    var inst = listType.InvokeMember("ToArray", System.Reflection.BindingFlags.InvokeMethod, null, listInst, null);
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
            else if (fieldType.IsEnum)
            {
                var str = o as string;
                object e = Enum.Parse(fieldType, str);
                f.SetValue(value,e);
            }
            else
            {
                Debug.Log("undefined type:" + fieldType);
            }
        }
    }
    /// <summary>
    /// デシリアライズ
    /// </summary>
    /// <typeparam name="T">タイプ</typeparam>
    /// <param name="jsonString">JSON文字列</param>
    /// <returns>T型の値</returns>
    public static T Deserialize<T>(string jsonString) where T : class
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
            else if (type.IsArray && (o is List<object>))
            {
                var array = o as List<object>;
                var value = (T)Activator.CreateInstance(type, array.Count);
                var arrayValue = value as Array;
                var elemType = type.GetElementType();
                for (int i = 0; i < array.Count; ++i)
                {
                    // if elemType
                    if (isSupportValueType(elemType))
                    {
                        arrayValue.SetValue(array[i], i);
                    }
                    else
                    {
                        var inst = Activator.CreateInstance(elemType);
                        deserializeObject(elemType, ref inst, array[i] as Dictionary<string, object>);
                        arrayValue.SetValue(inst, i);
                    }
                }
                return value;
            }
            else
            {
                var value = (T)Activator.CreateInstance(type);
                var inst = (object)value;
                deserializeObject(type, ref inst, o as Dictionary<string, object>);
                if (isStruct(type)) value = (T)inst;
                return value;
            }
        }
        catch (Exception ex)
        {
            Debug.Log(ex.Message);
            return default(T);
        }
    }
}
