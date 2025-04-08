# 概要
Unity向けのJSONシリアライザー＆デシリアライザーです。<BR>
かゆいところに手が届くことを目指して作りました。<BR>
Dictionaryに対応してます！<BR>
Unityの一般的な型に対応してます！<BR>
（Vector2,Vector3,Quaternion,Matrix4x4,Vector2Int,Vector3Int,Bounds,Plane,Ray,Ray2D,Rect,RectInt,Gradient,AnimationCurveなど。他に対応が必要であればリクエストしてください）<BR>
自作class,structなどの場合は、publicあるいは[SerializeField]部分を保存、復元します。<BR>

# インストール
トップメニューの Window -> Package Manager を選びます。<BR>
PackageManagerの左上の＋ボタンからAdd package from git URL を選びます。<BR>
URLに、https://github.com/sasame/DJSON.git を指定してください。

# 使い方
このように書けます。
```
class A
{
    public Vector2 value;
    public int number;
}

public void TestJSON_A()
{
    var a = new A();
    a.value = new Vector2(1.3f, -2.8f);
    a.number = 123;

    // シリアライズ
    var jsonString = DJSON.Serialize(a);
    Debug.Log(jsonString);

    // デシリアライズ
    var test = DJSON.Deserialize<A>(jsonString);
    Debug.Log(test.value);
    Debug.Log(test.number);
}
```

Dictionaryを使ったものでもいけます。
```
[System.Serializable]
class TestAnimation
{
    public Vector3 pos;
    public Vector3 rot;
    public Vector3 scl;
    public Dictionary<string, AnimationCurve> curves;
}

static Dictionary<string, TestAnimation> _testData2 = new Dictionary<string, TestAnimation>()
{
    {
        "ABC", new TestAnimation()
        {
            pos = new Vector3(1f, 2f, 3f),
            rot = new Vector3(10f, 15f, 45f),
            scl = new Vector3(1f, 2f, 1.5f),
            curves = new Dictionary<string,AnimationCurve>()
            {
                {
                    "rot.y",
                    new AnimationCurve(new Keyframe[]
                    {
                        new Keyframe(1f,2f,3f,4f),
                        new Keyframe(1.1f,1.2f,1.3f,1.4f),
                        new Keyframe(5f,4f,3f,2f),
                    })
                },
                {
                    "scl.x",
                    new AnimationCurve(new Keyframe[]
                    {
                        new Keyframe(1f,2f,3f,4f),
                        new Keyframe(1.1f,1.2f,1.3f,1.4f),
                        new Keyframe(4f,3f,2f,1f),
                        new Keyframe(0f,1f,2f,3f),
                        new Keyframe(5f,4f,3f,2f),
                    })
                },
            }
        }
    },
    {
        "DEF",
        new TestAnimation()
        {
            pos = Vector3.zero,
            rot = Vector3.zero,
            scl = Vector3.one,
            curves = new Dictionary<string, AnimationCurve>()
            {
                {
                    "pos.x",
                    new AnimationCurve(new Keyframe[]
                    {
                        new Keyframe(1f,2f),
                        new Keyframe(3f,4f),
                    })
                }
            }
        }
    }
};

[Test]
static public void Test_DJSON()
{
    var str = DJSON.Serialize(_testData2);
    Debug.Log(str);

    var o = DJSON.Deserialize<Dictionary<string, TestAnimation>>(str);
    Debug.Log(o);
}
```

SerializeFieldを使った場合。
```
class Class_Rect
{
    public Rect rect;
    [SerializeField] RectInt rectInt;

    public RectInt RectI
    {
        get { return rectInt; }
        set { rectInt = value; }
    }
}

[Test]
public void TestJSON_Rect()
{
    var o = new Class_Rect();
    o.rect = new Rect(1f,2f,3f,4f);
    o.RectI = new RectInt(1, 2, 3, 4);

    var jsonString = DJSON.Serialize(o);
    Debug.Log(jsonString);

    var test = DJSON.Deserialize<Class_Rect>(jsonString);
    Debug.Log(test.rect + ":");
    Debug.Log(test.RectI + ":");
}
```


## License
This project is licensed under the [MIT License](LICENSE).
