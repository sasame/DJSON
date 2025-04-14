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
    /// <summary>
    /// Unityの一部のデータ型のように通常シリアライズ不可能な型を別の型に相互変換する仕組みを提供する
    /// </summary>
    class Serializer
    {
        Type _replaceType;
        Func<object, object> _serialize;
        Func<object, object> _deserialize;

        public Type ReplaceType => _replaceType;
        public Func<object, object> Serialize => _serialize;
        public Func<object, object> Deserialize => _deserialize;

        public Serializer(Type repType,Func<object,object> serialize, Func<object, object> deserialize)
        {
            _replaceType = repType;
            _serialize = serialize;
            _deserialize = deserialize;
        }
    }

    // サポートするUnityの型(シリアライズ不可能な型)
    static Type[] _supportUnitySpecialTypes = new Type[]
    {
        typeof(Vector2Int),
        typeof(Vector3Int),
        typeof(Bounds),
        typeof(Plane),
        typeof(Ray),
        typeof(Ray2D),
        typeof(Rect),
        typeof(RectInt),
        typeof(Gradient),
        typeof(AnimationCurve),
    };
    static bool isSupportUnitySpecialType(Type type)
    {
        return _supportUnitySpecialTypes.Contains(type);
    }

    // シリアライズ可能なデータ型
#pragma warning disable 0649
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
    struct subPlane
    {
        public Vector3 normal;
        public float distance;
    }
    struct subRay
    {
        public Vector3 origin;
        public Vector3 direction;
    }
    struct subRay2D
    {
        public Vector2 origin;
        public Vector2 direction;
    }
    struct subRect
    {
        public float xMin;
        public float yMin;
        public float width;
        public float height;
    }
    struct subRectInt
    {
        public int xMin;
        public int yMin;
        public int width;
        public int height;
    }
    class subGradient
    {
        public GradientColorKey[] colorKeys;
        public GradientAlphaKey[] alphaKeys;
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
        public int tangentMode;

        public subKeyframe(Keyframe k)
        {
            time = k.time;
            value = k.value;
            inTangent = k.inTangent;
            outTangent = k.outTangent;
            inWeight = k.inWeight;
            outWeight = k.outWeight;
            weightedMode = k.weightedMode;
            tangentMode = k.tangentMode;
        }
        public Keyframe ToKeyframe()
        {
            var key = new Keyframe(time, value, inTangent, outTangent);
            key.tangentMode = tangentMode;
            return key;
        }
    }
    class subAnimationCurve
    {
        public subKeyframe[] keys;
        public WrapMode preWrapMode;
        public WrapMode postWrapMode;
    }
#pragma warning restore 0649

    static Dictionary<Type, Serializer> _dicUnitySerializer = null;
    static void initSerializer()
    {
        if (_dicUnitySerializer != null) return;

        _dicUnitySerializer = new Dictionary<Type, Serializer>();
        // Vector2Int
        _dicUnitySerializer[typeof(Vector2Int)] = new Serializer(typeof(subVector2Int),
            (o) => {
                var v = (Vector2Int)o;
                return new subVector2Int() { x = v.x, y = v.y };
            },
            (o) => {
                var sub = (subVector2Int)o;
                return new Vector2Int()
                {
                    x = sub.x,
                    y = sub.y,
                };
            });
        // Vector3Int
        _dicUnitySerializer[typeof(Vector3Int)] = new Serializer(typeof(subVector3Int),
            (o) => {
                var v = (Vector3Int)o;
                return new subVector3Int() { x = v.x, y = v.y, z = v.z };
            },
            (o) => {
                var sub = (subVector3Int)o;
                return new Vector3Int()
                {
                    x = sub.x,
                    y = sub.y,
                    z = sub.z,
                };
            });
        // Bounds
        _dicUnitySerializer[typeof(Bounds)] = new Serializer(typeof(subBounds),
            (o) => {
                var v = (Bounds)o;
                return new subBounds() { center = v.center, size = v.size };
            },
            (o) => {
                var sub = (subBounds)o;
                return new Bounds()
                {
                    center = sub.center,
                    size = sub.size
                };
            });
        // Plane
        _dicUnitySerializer[typeof(Plane)] = new Serializer(typeof(subPlane),
            (o) => {
                var v = (Plane)o;
                return new subPlane() { normal = v.normal, distance = v.distance };
            },
            (o) => {
                var v = (subPlane)o;
                return new Plane()
                {
                    normal = v.normal,
                    distance = v.distance
                };
            });
        // Ray
        _dicUnitySerializer[typeof(Ray)] = new Serializer(typeof(subRay),
            (o) => {
                var v = (Ray)o;
                return new subRay() { origin = v.origin, direction = v.direction };
            },
            (o) => {
                var v = (subRay)o;
                return new Ray()
                {
                    origin = v.origin,
                    direction = v.direction
                };
            });
        // Ray2D
        _dicUnitySerializer[typeof(Ray2D)] = new Serializer(typeof(subRay2D),
            (o) => {
                var v = (Ray2D)o;
                return new subRay2D() { origin = v.origin, direction = v.direction };
            },
            (o) => {
                var v = (subRay2D)o;
                return new Ray2D()
                {
                    origin = v.origin,
                    direction = v.direction
                };
            });
        // Rect
        _dicUnitySerializer[typeof(Rect)] = new Serializer(typeof(subRect),
            (o) => {
                var v = (Rect)o;
                return new subRect() { xMin = v.xMin, yMin = v.yMin, width = v.width, height = v.height };
            },
            (o) => {
                var v = (subRect)o;
                return new Rect(v.xMin, v.yMin, v.width, v.height);
            });
        // RectInt
        _dicUnitySerializer[typeof(RectInt)] = new Serializer(typeof(subRectInt),
            (o) => {
                var v = (RectInt)o;
                return new subRectInt() { xMin = v.xMin, yMin = v.yMin, width = v.width, height = v.height };
            },
            (o) => {
                var v = (subRectInt)o;
                return new RectInt(v.xMin, v.yMin, v.width, v.height);
            });
        // Gradient
        _dicUnitySerializer[typeof(Gradient)] = new Serializer(typeof(subGradient),
            (o) => {
                var v = (Gradient)o;
                return new subGradient() {
                    colorKeys = v.colorKeys,
                    alphaKeys = v.alphaKeys,
                    mode = v.mode
                };
            },
            (o) => {
                var v = (subGradient)o;
                return new Gradient()
                {
                    colorKeys = v.colorKeys,
                    alphaKeys = v.alphaKeys,
                    mode = v.mode
                };
            });
        // AnimationCurve
        _dicUnitySerializer[typeof(AnimationCurve)] = new Serializer(typeof(subAnimationCurve),
            (o) => {
                var v = (AnimationCurve)o;
                var aKeys = new subKeyframe[v.length];
                for (int i = 0; i < v.length; ++i) aKeys[i] = new subKeyframe(v.keys[i]);
                return new subAnimationCurve()
                {
                    keys = aKeys,
                    preWrapMode = v.preWrapMode,
                    postWrapMode = v.postWrapMode
                };
            },
            (o) => {
                var v = (subAnimationCurve)o;
                Keyframe[] vkeys = null;
                if (v.keys != null)
                {
                    vkeys = new Keyframe[v.keys.Length];
                    for (int i = 0; i < v.keys.Length; ++i) vkeys[i] = v.keys[i].ToKeyframe();
                }
                return new AnimationCurve()
                {
                    keys = vkeys,
                    preWrapMode = v.preWrapMode,
                    postWrapMode = v.postWrapMode,
                };
            });
    }

    /// <summary>
    /// サポート型が足りない場合に、追加する関数
    /// シリアライズ可能な型に相互変換するための関数を指定します
    /// </summary>
    /// <param name="type">対応する型（通常シリアライズ不可能な型）</param>
    /// <param name="replaceType">置き換える型（シリアライズ可能な型）</param>
    /// <param name="serialize">type型のデータをreplaceType型に変換する関数</param>
    /// <param name="deserizlize">replaceType型のデータをtype型に変換する関数</param>
    static void AppendSerializer(Type type,Type replaceType,Func<object,object> serialize,Func<object,object> deserizlize)
    {
        initSerializer();
        _dicUnitySerializer[type] = new Serializer(replaceType, serialize, deserizlize);
    }

    // Unityのシリアライズ不可能な型を、シリアライズ可能な型に変換
    static object convertSerializable(Type type,object value)
    {
        initSerializer();
        if (_dicUnitySerializer.TryGetValue(type,out var s))
        {
            return s.Serialize(value);
        }
        return null;
    }



    // Unityのシリアライズ不可能な型を、シリアライズ可能な型に変換しつつデシリアライズ
    static object convertDeserialize(Type fieldType, Dictionary<object, object> dic)
    {
        initSerializer();
        if (_dicUnitySerializer.TryGetValue(fieldType, out var s))
        {
            var inst = Activator.CreateInstance(s.ReplaceType);
            deserializeObject(s.ReplaceType, ref inst, dic);
            var sub = s.Deserialize(inst);
            return sub;
        }
        return null;
    }
}
