﻿using System;
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
    static Dictionary<string, object> serializeDictionary(Type type,object value)
    {
        Dictionary<string, object> dic = new Dictionary<string, object>();
        // ディクショナリ
        Type keyType = type.GetGenericArguments()[0];
        Type valueType = type.GetGenericArguments()[1];
        if (keyType == typeof(string))
        {
            var items = type.GetProperty("Keys", BindingFlags.Instance | BindingFlags.Public).GetValue(value) as IEnumerable;
            var values = type.GetProperty("Values", BindingFlags.Instance | BindingFlags.Public).GetValue(value) as ICollection;
            object[] keys = items.OfType<object>().ToArray();
            int indexKey = 0;
            foreach (var v in values)
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
        return dic;
    }

    /// <summary>
    /// classやstructを連想配列に変換
    /// </summary>
    /// <param name="type">タイプ</param>
    /// <param name="value">値</param>
    /// <returns>連想配列</returns>
    static Dictionary<string, object> serializeClassOrStruct(Type type,object value)
    {
        Dictionary<string, object> dic = new Dictionary<string, object>();

        // class or struct
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

    class subGradient
    {
        public GradientColorKey[] colorKeys;
        public GradientAlphaKey[] alphaKeys;
        public ColorSpace colorSpace;
        public GradientMode mode;
    };

    /// <summary>
    /// 連想配列への変換
    /// </summary>
    /// <param name="type">タイプ</param>
    /// <param name="value">値</param>
    /// <returns>連想配列</returns>
    static Dictionary<string, object> serializeObject(Type type, object value)
    {
        if (value == null) return null;

        if (isSupportUnitySpecialType(type))
        {
            if (type == typeof(Gradient))
            {
                var g = value as Gradient;
                var valueG = new subGradient()
                {
                    colorKeys= g.colorKeys,
                    alphaKeys=g.alphaKeys,
                    colorSpace = g.colorSpace,
                    mode = g.mode
                };
                return serializeClassOrStruct(valueG.GetType(), valueG);
            }
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

    public static string Serialize<T>(T value)
    {
        Type type = typeof(T);

        if (type.IsArray)
        {
            var result = serializeArray(value);
            return ToJson(result);
        }
        else
        {
            // objectタイプ
            var result = serializeObject(type, value);
            return ToJson(result);
        }
    }
}
