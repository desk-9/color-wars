using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public delegate void Callback();
public delegate int IntCallback();
public delegate void ColorSetter(Color color);
public delegate void FloatSetter(float floatValue);
public delegate bool Predicate();

public enum LogLevel
{
    Normal,
    Warning,
    Error
}

namespace UtilityExtensions
{
    // This namespace is for any general utility extensions to existing classes

    public static class UtilityExtensionsContainer
    {
        // TODO dkonik: Does this actually fucking work? lol static dynamic
        public static dynamic ThrowIfNull(this Component component, string exceptionMessage = null)
        {
            if (component == null)
            {
                if (exceptionMessage != null)
                {
                    throw new Exception(exceptionMessage);
                } else
                {
                    throw new Exception();
                }
            } else
            {
                return component;
            }
        }

        public static dynamic ThrowIfNull(this GameObject gameObject, string exceptionMessage = null)
        {
            if (gameObject == null)
            {
                if (exceptionMessage != null)
                {
                    throw new Exception(exceptionMessage);
                }
                else
                {
                    throw new Exception();
                }
            }
            else
            {
                return gameObject;
            }
        }

        public static T EnsureComponent<T>(this GameObject game_object) where T : Component
        {
            T component = game_object.GetComponent<T>();
            if (component == null)
            {
                throw new MissingComponentException("Component missing");
            }
            return component;
        }

        public static T EnsureComponent<T>(this Component other_component) where T : Component
        {
            T component = other_component.GetComponent<T>();
            if (component == null)
            {
                throw new MissingComponentException("Component missing");
            }
            return component;
        }

        public static void FrameDelayCall(this MonoBehaviour component, Callback function,
                                          int frames = 1)
        {
            component.StartCoroutine(
                CoroutineUtility.RunThenCallback(
                    CoroutineUtility.WaitForFrames(frames), function));
        }

        public static void TimeDelayCall(this MonoBehaviour component, Callback function,
                                         float seconds = 1)
        {
            component.StartCoroutine(
                CoroutineUtility.RunThenCallback(
                    CoroutineUtility.WaitForSeconds(seconds), function));
        }

        public static void RealtimeDelayCall(this MonoBehaviour component, Callback function,
                                             float seconds = 1)
        {
            component.StartCoroutine(
                CoroutineUtility.RunThenCallback(
                    CoroutineUtility.WaitForRealtimeSeconds(seconds), function));
        }

        public static T FindComponent<T>(this Transform transform, string name) where T : Component
        {
            Transform thing = transform.Find(name);
            T component = thing?.gameObject.GetComponent<T>();
            return component;
        }

        public static T FindComponent<T>(this GameObject go, string name) where T : Component
        {
            Transform thing = go.transform.Find(name);
            T component = thing?.gameObject.GetComponent<T>();
            return component;
        }

        public static T FindComponent<T>(this Component comp, string name) where T : Component
        {
            Transform thing = comp.transform.Find(name);
            T component = thing?.gameObject.GetComponent<T>();
            return component;
        }

        public static GameObject FindChild(this GameObject thing, string name)
        {
            return thing.transform.Find(name).gameObject;
        }

        public static void Add<T1, T2>(this IList<Tuple<T1, T2>> list, T1 item1, T2 item2)
        {
            list.Add(Tuple.Create(item1, item2));
        }

        public static void Add<T1, T2, T3>(this IList<Tuple<T1, T2, T3>> list, T1 item1, T2 item2, T3 item3)
        {
            list.Add(Tuple.Create(item1, item2, item3));
        }

        public static void Add(this IList<TutorialStageInfo> list,
                               string a, string b, string c)
        {
            list.Add(new TutorialStageInfo(a, b, c));
        }

        public static void Add(this IList<TutorialStageInfo> list,
                               string a, string b, string c, TutorialRequirement requirement)
        {
            list.Add(new TutorialStageInfo(a, b, c, requirement));
        }

        public static void Add(this IList<SubclipInfo> list,
                               string text, float time)
        {
            list.Add(new SubclipInfo(text, time));
        }

        public static void Add(this IList<SubclipInfo> list,
                               string text)
        {
            list.Add(new SubclipInfo(text));
        }

        public static void Add(this IList<LiveClipInfo> list,
                               string name, List<SubclipInfo> subclips)
        {
            list.Add(new LiveClipInfo(name, subclips));
        }

        public static void Add(this IList<LiveClipInfo> list,
                               string name, List<SubclipInfo> subclips, float pre, float post)
        {
            list.Add(new LiveClipInfo(name, subclips, pre, post));
        }


        public static EffectSpawner FindEffect(this Component component, EffectType type)
        {
            EffectSpawner[] effects = component.GetComponents<EffectSpawner>();
            foreach (EffectSpawner effect in effects)
            {
                if (effect.effectType == type)
                {
                    return effect;
                }
            }
            return null;
        }

        public static TValue GetDefault<TValue, TKey>(
            this Dictionary<TKey, TValue> dict, TKey key, TValue defaultValue)
        {
            if (dict.ContainsKey(key))
            {
                return dict[key];
            }
            else
            {
                return defaultValue;
            }
        }
    }
}


public static class Utility
{
    // Class for any static utility functions
    public static IntCallback ModCycle(int start, int modulus)
    {
        int value = start;
        return () =>
        {
            int result = value;
            value = (value + 1) % modulus;
            return result;
        };
    }

    // Ripped from
    // https://answers.unity.com/questions/661383/whats-the-most-efficient-way-to-rotate-a-vector2-o.html
    public static Vector2 RotateVector(this Vector2 v, float degrees)
    {
        float sin = Mathf.Sin(degrees * Mathf.Deg2Rad);
        float cos = Mathf.Cos(degrees * Mathf.Deg2Rad);

        float tx = v.x;
        float ty = v.y;
        v.x = (cos * tx) - (sin * ty);
        v.y = (sin * tx) + (cos * ty);
        return v;
    }

    public static Color ColorComplement(Color baseColor)
    {
        float h;
        float s;
        float v;
        Color.RGBToHSV(baseColor, out h, out s, out v);
        float shiftedH = h + 0.5f;
        h = shiftedH - (shiftedH > 1 ? 1 : 0);
        return Color.HSVToRGB(h, s, v);
    }

    public struct HSVColor
    {
        public float h;
        public float s;
        public float v;
        public HSVColor(float h, float s, float v)
        {
            this.h = h;
            this.s = s;
            this.v = v;
        }

        public HSVColor(Color color)
        {
            Color.RGBToHSV(color, out h, out s, out v);
        }

        public Color ToColor()
        {
            return Color.HSVToRGB(h, s, v);
        }
    }




    public static void Toggle(GameObject gameObj)
    {
        gameObj.SetActive(!gameObj.activeInHierarchy);
    }

    public static void Toggle(MonoBehaviour component)
    {
        component.enabled = !component.enabled;
    }

    public static string UniqueString()
    {
        return System.Guid.NewGuid().ToString();
    }

    public static void Blowback(Vector2 center, float radius,
                                float blowback_strength, bool blowback_is_velocity = false,
                                int layerMask = Physics2D.DefaultRaycastLayers,
                                GameObjectCallback onCollided = null,
                                HashSet<GameObject> excludes = null)
    {
        // Usage
        //
        // When `blowback_is_velocity` is false, the `blowback` parameter
        // will be added as a force. When it's true, the force required to reach
        // a velocity given by `blowback` based on collided objects mass will be
        // used.
        //
        // onCollided will be called on each collided object.
        if (onCollided == null)
        {
            onCollided = delegate { };
        }
        if (excludes == null)
        {
            excludes = new HashSet<GameObject>();
        }
        Collider2D[] collided = Physics2D.OverlapCircleAll(center, radius, layerMask);
        foreach (Collider2D collider in collided)
        {
            GameObject thing = collider.gameObject;
            if (excludes.Contains(thing))
            {
                continue;
            }
            Rigidbody2D rigidbody = thing.GetComponent<Rigidbody2D>();
            if (rigidbody != null)
            {
                onCollided(thing);
                Vector2 direction = (Vector2)rigidbody.transform.position - center;
                Vector2 knockback = direction * blowback_strength;
                if (blowback_is_velocity)
                {
                    knockback = knockback * rigidbody.mass;
                }
                rigidbody.AddForce(knockback, ForceMode2D.Impulse);
            }
        }
    }

    public static void BlowbackPlayers(Vector2 center, float radius,
                                       float blowback_strength,
                                       bool blowback_is_velocity = false,
                                       HashSet<GameObject> excludes = null,
                                       float? stunTime = null)
    {
        GameObjectCallback stunPlayer = (GameObject thing) =>
        {
            PlayerStateManager player = thing.GetComponent<PlayerStateManager>();
            PlayerStun stun = thing.GetComponent<PlayerStun>();
            if (player != null && stun != null)
            {
                player.AttemptStun(
                    () => stun.StartStun(null, stunTime), stun.StopStunned);
            }
        };
        Blowback(center, radius, blowback_strength, blowback_is_velocity,
                 LayerMask.GetMask("Player"), stunPlayer, excludes);
    }

    public static void BlowbackFromPlayer(GameObject player, float radius,
                                          float blowback_strength,
                                          bool blowback_is_velocity = false,
                                          float? stunTime = null)
    {

        HashSet<GameObject> ignoreSet = new HashSet<GameObject>() { player.gameObject };
        if (player.GetComponent<Player>().team != null)
        {
            ignoreSet = new HashSet<GameObject>(
                player.GetComponent<Player>().team.teamMembers.Select(p => p.gameObject)
                );
        }
        BlowbackPlayers(player.transform.position, radius,
                        blowback_strength, blowback_is_velocity,
                        ignoreSet,
                        stunTime);
    }


    public static void Print(params object[] args)
    {
        LogLevel logLevel = LogLevel.Normal;
        int argsEnd = args.Length;
        if (args.Length > 1)
        {
            object last = args[args.Length - 1];
            if (last.GetType() == typeof(LogLevel))
            {
                logLevel = (LogLevel)last;
                argsEnd -= 1;
            }
        }
        string formatString = "";
        for (int i = 0; i < argsEnd; i++)
        {
            formatString += string.Format("{{{0}}} ", i);
        }
        if (logLevel == LogLevel.Normal)
        {
            Debug.LogFormat(formatString, args);
        }
        else if (logLevel == LogLevel.Warning)
        {
            Debug.LogWarningFormat(formatString, args);
        }
        else if (logLevel == LogLevel.Error)
        {
            Debug.LogErrorFormat(formatString, args);
        }
    }

    public static void ChangeTimeScale(float factor)
    {
        Time.timeScale = 1 * factor;
        Time.fixedDeltaTime = 0.02f * factor;
    }
    public static float NormalizeDegree(float degree)
    {
        if (degree <= 0)
        {
            degree = 360 - Mathf.Repeat(-degree, 360);
        }
        else
        {
            degree = Mathf.Repeat(degree, 360);
        }
        return degree;
    }
}

public class ModCycle
{
    public int nextValue = 0;
    public int modulus;

    public ModCycle(int start, int modulus)
    {
        nextValue = start;
        this.modulus = modulus;
    }

    public int PeekNext()
    {
        return nextValue;
    }

    public int Next()
    {
        int result = nextValue;
        nextValue = (nextValue + 1) % modulus;
        return result;
    }

}


public class CoroutineUtility : MonoBehaviour
{
    // Class for utility functions involving Coroutines.
    //
    // Any functions that simply return IEnumerators/don't call StartCoroutine
    // should be static. Functions that do call StartCoroutine need to be
    // non-static and called through the singleton instance.

    public static CoroutineUtility instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public static IEnumerator RunThenCallback(IEnumerator coroutine, Callback callback)
    {
        yield return coroutine;
        callback();
    }

    public static IEnumerator RunSequentially(params IEnumerator[] coroutines)
    {
        for (int i = 0; i < coroutines.Length; ++i)
        {
            yield return coroutines[i];
        }
    }

    public static IEnumerator WaitForFrames(int frames = 1)
    {
        for (int i = 0; i < frames; i++)
        {
            yield return null;
        }
    }

    public static IEnumerator WaitForFixedUpdates(int updates = 1)
    {
        for (int i = 0; i < updates; i++)
        {
            yield return new WaitForFixedUpdate();
        }
    }

    // A version of WaitForSeconds that returns an IEnumerator rather than the
    // custom class instance
    public static IEnumerator WaitForSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
    }

    public static IEnumerator WaitForRealtimeSeconds(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
    }

}
