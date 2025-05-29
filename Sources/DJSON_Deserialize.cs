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
    /// <param name="fieldType">リストのフィールドタイプ</param>
    /// <param name="type">データタイプ</param>
    /// <param name="value">リストオブジェクト</param>
    /// <param name="list">データソースのリスト</param>
    static void deserializeList(Type fieldType, Type type, object value, List<object> list)
    {
        foreach (var item in list)
        {
            if (item == null)
            {
                fieldType.InvokeMember("Add", System.Reflection.BindingFlags.InvokeMethod, null, value, new object[] { null });
                continue;
            }

            var itemType = type;
            if (type == typeof(object))
            {
                itemType = value.GetType();
            }

            if (item == null)
            {
                fieldType.InvokeMember("Add", System.Reflection.BindingFlags.InvokeMethod, null, value, new object[] { null });
            }
            else if (type == typeof(string))
            {
                var inst = item as string;
                fieldType.InvokeMember("Add", System.Reflection.BindingFlags.InvokeMethod, null, value, new object[] { inst });
            }
            else if (isSupportValueType(itemType))
            {
                fieldType.InvokeMember("Add", System.Reflection.BindingFlags.InvokeMethod, null, value, new object[] { Convert.ChangeType(item,type) });
            }
            else if (isSupportUnitySpecialType(itemType))
            {
                var inst = convertDeserialize(itemType, item as Dictionary<object, object>);
                fieldType.InvokeMember("Add", System.Reflection.BindingFlags.InvokeMethod, null, value, new object[] { inst });
            }
            else
            {
                var inst = Activator.CreateInstance(itemType);
                if (item is Dictionary<object, object>)
                {
                    deserializeObject(itemType, ref inst, item as Dictionary<object, object>);
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
    static void deserializeDictionary(Type fieldType, object value, Dictionary<object, object> dic)
    {
        var keyType = fieldType.GetGenericArguments()[0];
        var valueType = fieldType.GetGenericArguments()[1];
        // objectタイプの場合
        if (valueType == typeof(object))
        {
            foreach (var pair in dic)
            {
                if (pair.Value == null) continue;
                var t = pair.Value.GetType();
                if (isSupportValueType(t))
                {
                    fieldType.InvokeMember("Add", System.Reflection.BindingFlags.InvokeMethod, null, value, new object[] { pair.Key, pair.Value });
                }
                else if ((fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>)) && (pair.Value is List<object>))
                {
                    var listInst = Activator.CreateInstance(fieldType);
                    var typeItem = fieldType.GetGenericArguments()[0];
                    deserializeList(fieldType, typeItem, listInst, pair.Value as List<object>);
                    fieldType.InvokeMember("Add", System.Reflection.BindingFlags.InvokeMethod, null, value, new object[] { pair.Key, listInst });
                }
                else if ((fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(Dictionary<,>)) && (pair.Value is Dictionary<object, object>))
                {
                    var dicInst = Activator.CreateInstance(fieldType);
                    deserializeDictionary(fieldType, dicInst, pair.Value as Dictionary<object, object>);
                    fieldType.InvokeMember("Add", System.Reflection.BindingFlags.InvokeMethod, null, value, new object[] { pair.Key, dicInst });
                }
            }
            return;
        }

        //
        if (isSupportUnitySpecialType(valueType))
        {
            foreach (var pair in dic)
            {
                var inst = convertDeserialize(valueType, pair.Value as Dictionary<object, object>);
                fieldType.InvokeMember("Add", System.Reflection.BindingFlags.InvokeMethod, null, value, new object[] { pair.Key, inst });
            }
        }
        else if (valueType == typeof(string))
        {
            foreach (var pair in dic)
            {
                var key = Convert.ChangeType(pair.Key, keyType);
                fieldType.InvokeMember("Add", System.Reflection.BindingFlags.InvokeMethod, null, value, new object[] { key, pair.Value });
            }
        }
        else if (valueType == typeof(bool))
        {
            foreach (var pair in dic)
            {
                var key = Convert.ChangeType(pair.Key, keyType);
                fieldType.InvokeMember("Add", System.Reflection.BindingFlags.InvokeMethod, null, value, new object[] { key, pair.Value });
            }
        }
        else if (valueType.IsClass)
        {
            foreach (var pair in dic)
            {
                var inst = Activator.CreateInstance(valueType);
                deserializeObject(valueType, ref inst, pair.Value as Dictionary<object, object>);
                var key = Convert.ChangeType(pair.Key, keyType);
                fieldType.InvokeMember("Add", System.Reflection.BindingFlags.InvokeMethod, null, value, new object[] { key, inst });
            }
        }
        else if (isStruct(valueType))
        {
            foreach (var pair in dic)
            {
                var inst = Activator.CreateInstance(valueType);
                deserializeObject(valueType, ref inst, pair.Value as Dictionary<object, object>);
                var key = Convert.ChangeType(pair.Key, keyType);
                fieldType.InvokeMember("Add", System.Reflection.BindingFlags.InvokeMethod, null, value, new object[] { key, inst });
            }
        }
        else if (isSupportValueType(valueType))
        {
            foreach (var pair in dic)
            {
                var key = Convert.ChangeType(pair.Key, keyType);
                fieldType.InvokeMember("Add", System.Reflection.BindingFlags.InvokeMethod, null, value, new object[] { key, Convert.ChangeType(pair.Value,valueType) });
            }
        }

    }
    /// <summary>
    /// オブジェクトをデシリアライズ
    /// </summary>
    /// <param name="type">オブジェクトのデータ型</param>
    /// <param name="value">値</param>
    /// <param name="dic">データソースのディクショナリ(連想配列)</param>
    static void deserializeObject(Type type, ref object value, Dictionary<object, object> dic)
    {
        var fields = getSerializableFields(type);// type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        foreach (var f in fields)
        {
            if (dic.ContainsKey(f.Name) == false) continue;
            var o = dic[f.Name];
            if (o == null) continue;
            var fieldType = f.FieldType;
            if (isSupportUnitySpecialType(fieldType))
            {
                var ret = convertDeserialize(fieldType, o as Dictionary<object, object>);
                f.SetValue(value,ret);
            }
            else if ((fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>)) && (o is List<object>))
            {
                //                var listInst = f.GetValue(value);
                var listInst = Activator.CreateInstance(fieldType);
                var typeItem = fieldType.GetGenericArguments()[0];
                deserializeList(fieldType, typeItem, listInst, o as List<object>);
                f.SetValue(value, listInst);
            }
            else if ((fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(Dictionary<,>)) && (o is Dictionary<object, object>))
            {
                var inst = Activator.CreateInstance(fieldType);
                deserializeDictionary(fieldType, inst, o as Dictionary<object, object>);
                f.SetValue(value, inst);
            }
            else if (isSupportValueType(fieldType))
            {
                f.SetValue(value, Convert.ChangeType(o,fieldType));
            }
            else if (fieldType.IsClass)
            {
                if (fieldType.IsArray)
                {
                    var listType = Type.GetType("System.Collections.Generic.List`1[[" + fieldType.GetElementType().AssemblyQualifiedName + "]]");
                    var listInst = Activator.CreateInstance(listType);
                    deserializeList(listType, fieldType.GetElementType(), listInst, o as List<object>);
                    var inst = listType.InvokeMember("ToArray", System.Reflection.BindingFlags.InvokeMethod, null, listInst, null);
                    f.SetValue(value, inst);
                }
                else
                {
                    var inst = Activator.CreateInstance(fieldType);
                    deserializeObject(fieldType, ref inst, o as Dictionary<object, object>);
                    f.SetValue(value, inst);
                }
            }
            else if (isStruct(fieldType))
            {
                var inst = f.GetValue(value);
                deserializeObject(fieldType, ref inst, o as Dictionary<object, object>);
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
    public static T Deserialize<T>(string jsonString) 
    {
        var o = Parse(jsonString);
        var type = typeof(T);
        if (o == null)
        {
            return (T)o;
        } else if ((type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)) && (o is List<object>))
        {
            var list = o as List<object>;
            var value = (T)Activator.CreateInstance(type);
            deserializeList(type, type, value, list);
            return value;
        }
        else if ((type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>)) && (o is Dictionary<object, object>))
        {
            var inst = Activator.CreateInstance(type);
            deserializeDictionary(type, inst, o as Dictionary<object, object>);
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
                    deserializeObject(elemType, ref inst, array[i] as Dictionary<object, object>);
                    arrayValue.SetValue(inst, i);
                }
            }
            return value;
        }else if (type.IsEnum)
        {
            var str = o as string;
            object e = Enum.Parse(type, str);
            return (T)e;
        }
        else if (isSupportUnitySpecialType(type))
        {
            var ret = convertDeserialize(type, o as Dictionary<object, object>);
            return (T)ret;
        }
        else
        {
            var value = (T)Activator.CreateInstance(type);
            var inst = (object)value;
            deserializeObject(type, ref inst, o as Dictionary<object, object>);
            if (isStruct(type)) value = (T)inst;
            return value;
        }
    }
}
