using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;

/// <summary>
/// JSON変換　シリアライズ処理
/// Unityのシリアライズ不可能な型を、シリアライズ可能な型に変換して処理する部分
/// </summary>
public partial class DJSON
{
    // サポートするUnityの型(シリアライズ不可能な型)
    static Type[] _supportUnitySpecialTypes = new Type[]
    {
        typeof(Vector2Int),
        typeof(Vector3Int),
        typeof(Bounds),
        // Plane 
        // Ray
        // Ray2D 
        // Rect 
        // RectInt 
        typeof(Gradient),
        typeof(AnimationCurve),
    };
    static bool isSupportUnitySpecialType(Type type)
    {
        return _supportUnitySpecialTypes.Contains(type);
    }

    // シリアライズ可能なデータ型
    struct subVector2Int
    {
        public int x;
        public int y;
    }
    struct subVector3Int
    {
        public int x;
        public int y;
        public int z;
    }
    struct subBounds
    {
        public Vector3 center;
        public Vector3 size;
    }
    class subGradient
    {
        public GradientColorKey[] colorKeys;
        public GradientAlphaKey[] alphaKeys;
        public ColorSpace colorSpace;
        public GradientMode mode;
    };
    struct subKeyframe
    {
        public float time;
        public float value;
        public float inTangent;
        public float outTangent;
        public float inWeight;
        public float outWeight;
        public WeightedMode weightedMode;

        public subKeyframe(Keyframe k)
        {
            time = k.time;
            value = k.value;
            inTangent = k.inTangent;
            outTangent = k.outTangent;
            inWeight = k.inWeight;
            outWeight = k.outWeight;
            weightedMode = k.weightedMode;
        }
        public Keyframe ToKeyframe()
        {
            return new Keyframe(time, value, inTangent, outTangent, inWeight, outWeight);
        }
    }
    class subAnimationCurve
    {
        public subKeyframe[] keys;
        public WrapMode preWrapMode;
        public WrapMode postWrapMode;
    }

    // Unityのシリアライズ不可能な型を、シリアライズ可能な型に変換
    static object convertSerializable(Type type,object value)
    {
        if (type == typeof(Vector2Int))
        {
            var v = (Vector2Int)value;
            return new subVector2Int() { x = v.x, y = v.y };
        }else if (type == typeof(Vector3Int))
        {
            var v = (Vector3Int)value;
            return new subVector3Int() { x = v.x, y = v.y,z = v.z };
        }
        else if (type == typeof(Bounds))
        {
            var v = (Bounds)value;
            return new subBounds() { center = v.center, size = v.size};
        }
        else if (type == typeof(Gradient))
        {
            var g = value as Gradient;
            var valueG = new subGradient()
            {
                colorKeys = g.colorKeys,
                alphaKeys = g.alphaKeys,
                colorSpace = g.colorSpace,
                mode = g.mode
            };
            return valueG;
        }
        else if (type == typeof(AnimationCurve))
        {
            var a = value as AnimationCurve;
            var aKeys = new subKeyframe[a.length];
            for (int i = 0; i < a.length; ++i) aKeys[i] = new subKeyframe(a.keys[i]);
            var valueA = new subAnimationCurve()
            {
                keys = aKeys,
                preWrapMode = a.preWrapMode,
                postWrapMode = a.postWrapMode
            };
            return valueA;
        }
        return null;
    }

    // Unityのシリアライズ不可能な型を、シリアライズ可能な型に変換しつつデシリアライズ
    static void convertDeserialize(FieldInfo f,object value, Dictionary<string, object> dic)
    {
        var fieldType = f.FieldType;
        if (fieldType == typeof(Vector2Int))
        {
            var sub = new subVector2Int();
            object subO = sub;
            deserializeObject(typeof(subVector2Int), ref subO, dic);
            sub = (subVector2Int)subO;
            f.SetValue(value, new Vector2Int() { 
                x = sub.x,
                y = sub.y,
            });
        }
        else if (fieldType == typeof(Vector3Int))
        {
            var sub = new subVector3Int();
            object subO = sub;
            deserializeObject(typeof(subVector3Int), ref subO, dic);
            sub = (subVector3Int)subO;
            f.SetValue(value, new Vector3Int()
            {
                x = sub.x,
                y = sub.y,
                z = sub.z,
            });
        }
        else if (fieldType == typeof(Bounds))
        {
            var sub = new subBounds();
            object subO = sub;
            deserializeObject(typeof(subBounds), ref subO, dic);
            sub = (subBounds)subO;
            f.SetValue(value, new Bounds()
            {
                center = sub.center,
                size= sub.size
            });
        }
        else if (fieldType == typeof(Gradient))
        {
            var sub = new subGradient();
            object subO = sub;
            deserializeObject(typeof(subGradient), ref subO, dic);
            f.SetValue(value, new Gradient()
            {
                colorKeys = sub.colorKeys,
                alphaKeys = sub.alphaKeys,
                colorSpace = sub.colorSpace,
                mode = sub.mode,
            });
        }
        else if (fieldType == typeof(AnimationCurve))
        {
            var sub = new subAnimationCurve();
            object subO = sub;
            deserializeObject(typeof(subAnimationCurve), ref subO, dic);
            var vkeys = new Keyframe[sub.keys.Length];
            for (int i = 0; i < sub.keys.Length; ++i) vkeys[i] = sub.keys[i].ToKeyframe();
            f.SetValue(value, new AnimationCurve()
            {
                keys = vkeys,
                preWrapMode = sub.preWrapMode,
                postWrapMode = sub.postWrapMode,
            });
        }
    }
}
