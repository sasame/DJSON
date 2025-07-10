using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;

/// <summary>
/// JSON変換　シリアライズ処理
/// </summary>
public partial class DJSON
{
    /// <summary>
    /// public或いは、[SerializeField]のフィールドを取得
    /// </summary>
    /// <param name="type">タイプ</param>
    /// <returns>フィールド配列</returns>
    static FieldInfo[] getSerializableFields(Type type)
    {
        return type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(field =>
                field.IsPublic ||
                field.GetCustomAttribute<SerializeField>() != null)
            .ToArray();
    }

    /// <summary>
    /// 配列をシリアライズ
    /// </summary>
    /// <param name="fieldValue">フィールドの値</param>
    /// <returns>データリスト</returns>
    static List<object> serializeArray(object fieldValue)
    {
        // array
        var list = new List<object>();
        var fieldList = fieldValue as Array;
        foreach (var item in fieldList)
        {
            if (item != null)
            {
                if (isSupportValueType(item.GetType()))
                {
                    list.Add(item);
                }
                else
                {
                    var arrayItem = serializeObject(item.GetType(), item);
                    list.Add(arrayItem);
                }
            }
            else
            {
                list.Add(null);
            }
        }
        return list;
    }

    /// <summary>
    /// Dictionaryを連想配列に変換
    /// </summary>
    /// <param name="type">タイプ</param>
    /// <param name="value">値</param>
    /// <returns>連想配列</returns>
    static Dictionary<object, object> serializeDictionary(Type type,object value)
    {
        Dictionary<object, object> dic = new Dictionary<object, object>();
        // ディクショナリ
        Type keyType = type.GetGenericArguments()[0];
        Type valueType = type.GetGenericArguments()[1];
//        if (keyType == typeof(string))
        if (isSupportValueType(keyType) || (keyType == typeof(System.Object)))
        {
            var items = type.GetProperty("Keys", BindingFlags.Instance | BindingFlags.Public).GetValue(value) as IEnumerable;
            var values = type.GetProperty("Values", BindingFlags.Instance | BindingFlags.Public).GetValue(value) as ICollection;
            object[] keys = items.OfType<object>().ToArray();
            int indexKey = 0;
            foreach (var v in values)
            {
                var key = keys[indexKey];
                if (v == null)
                {
                    dic[key] = v;
                }
                else
                {
                    Type thisValueType = valueType;
                    if (valueType == typeof(System.Object))
                    {
                        thisValueType = v.GetType();
                    }

                    if (isSupportValueType(thisValueType))
                    {
                        dic[key] = v;
                    }
                    else if (thisValueType.IsGenericType && thisValueType.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        dic[key] = serializeList(thisValueType, v);
                    }
                    else if (thisValueType.IsClass)
                    {
                        dic[key] = serializeObject(thisValueType, v);
                    }
                    else if (isStruct(thisValueType))
                    {
                        dic[key] = serializeObject(thisValueType, v);
                    }
                    else
                    {
                        UnityEngine.Debug.Log("ignore type:" + thisValueType);
                    }
                }
                ++indexKey;
            }
        }
        return dic;
    }

    static List<object> serializeList(Type fieldType,object fieldValue)
    {
        // list
        var list = new List<object>();
        var fieldList = fieldValue as IList;
        foreach (var item in fieldList)
        {
            if (item == null)
            {
                list.Add(null);
                continue;
            }
            var itemType = item.GetType();

            if (itemType == typeof(string))
            {
                list.Add(item);
            }
            else if (isSupportValueType(itemType))
            {
                list.Add(item);
            }
            else if (itemType.IsGenericType && itemType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                list.Add(serializeDictionary(itemType, item));
            }
            else if (isStruct(itemType))
            {
                list.Add(serializeObject(itemType, item));
            }
            else if (itemType.IsClass)
            {
                list.Add(serializeObject(itemType, item));
            }
        }
        return list;
    }

    /// <summary>
    /// classやstructを連想配列に変換
    /// </summary>
    /// <param name="type">タイプ</param>
    /// <param name="value">値</param>
    /// <returns>連想配列</returns>
    static Dictionary<object, object> serializeClassOrStruct(Type type,object value)
    {
        Dictionary<object, object> dic = new Dictionary<object, object>();

        // class or struct
        var fields = getSerializableFields(type);// type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
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
                dic[f.Name] = serializeList(fieldType, fieldValue);
            }
            else if (fieldType.IsArray)
            {
                // array
                dic[f.Name] = serializeArray(fieldValue);
            }
            else if (fieldType.IsClass)
            {
                dic[f.Name] = serializeObject(fieldType, fieldValue);
            }
            else if (isStruct(fieldType))
            {
                dic[f.Name] = serializeObject(fieldType, fieldValue);
            }
            else if (fieldType.IsEnum)
            {
                dic[f.Name] = fieldValue.ToString();
            }
            else
            {
                Debug.LogWarning("Unsupported type:" + fieldType);
            }
        }
        return dic;
    }

    /// <summary>
    /// 連想配列への変換
    /// </summary>
    /// <param name="type">タイプ</param>
    /// <param name="value">値</param>
    /// <returns>連想配列</returns>
    static Dictionary<object, object> serializeObject(Type type, object value)
    {
        if (value == null) return null;

        if (isSupportUnitySpecialType(type))
        {
            var conv = convertSerializable(type, value);
            if (conv == null) return null;
            return serializeClassOrStruct(conv.GetType(), conv);
        }
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            return serializeDictionary(type, value);
        }
        else
        {
            return serializeClassOrStruct(type, value);
        }

        Debug.LogWarning("Unsuported type:" + type);
        return null;
    }

    /// <summary>
    /// シリアライズ
    /// </summary>
    /// <typeparam name="T">データ型</typeparam>
    /// <param name="value">値</param>
    /// <param name="omitNullDictionaryValues">連想配列の値がnullのときは、保存しないフラグ</param>
    /// <returns>JSON文字列</returns>
    public static string Serialize<T>(T value,bool omitNullDictionaryValues = true)
    {
        Type type = typeof(T);

        if (value == null)
        {
            return "null";
        }else if (type.IsArray)
        {
            var result = serializeArray(value);
            return ToJson(result, omitNullDictionaryValues);
        }
        else if (type.IsEnum)
        {
            return value.ToString();
        }
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            return ToJson(serializeList(type,value), omitNullDictionaryValues);
        }
        else if (isSupportValueType(type))
        {
            return ToJson(value);
        }
        else if (value != null)
        {
            if (isSupportValueType(value.GetType()))
            {
                return ToJson(value);
            }
            else
            {
                // objectタイプ valueのtype利用
                var result = serializeObject(value.GetType(),value);
                return ToJson(result, omitNullDictionaryValues);
            }
        }
        else
        {
            // objectタイプ
            var result = serializeObject(type, value);
            return ToJson(result, omitNullDictionaryValues);
        }
    }
}
