using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate void Callback();
public delegate int IntCallback();

namespace UtilityExtensions {
    // This namespace is for any general utility extensions to existing classes

    public static class UtilityExtensionsContainer {

        public static T EnsureComponent<T>(this GameObject game_object) where T : Component {
            var component = game_object.GetComponent<T>();
            if (component == null) {
                throw new MissingComponentException("Component missing");
            }
            return component;
        }

        public static T EnsureComponent<T>(this Component other_component) where T : Component {
            var component = other_component.GetComponent<T>();
            if (component == null) {
                throw new MissingComponentException("Component missing");
            }
            return component;
        }

        public static void FrameDelayCall(this MonoBehaviour component, Callback function,
                                          int frames = 1) {
            component.StartCoroutine(
                CoroutineUtility.RunThenCallback(
                    CoroutineUtility.WaitForFrames(frames), function));
        }

        public static void TimeDelayCall(this MonoBehaviour component, Callback function,
                                         float seconds = 1) {
            component.StartCoroutine(
                CoroutineUtility.RunThenCallback(
                    CoroutineUtility.WaitForSeconds(seconds), function));
        }

        public static T FindComponent<T>(this Transform transform, string name) where T : Component {
            var thing = transform.Find(name);
            var component = thing?.gameObject.GetComponent<T>();
            return component;
        }

        public static T FindComponent<T>(this GameObject go, string name) where T : Component {
            var thing = go.transform.Find(name);
            var component = thing?.gameObject.GetComponent<T>();
            return component;
        }

        public static T FindComponent<T>(this Component comp, string name) where T : Component {
            var thing = comp.transform.Find(name);
            var component = thing?.gameObject.GetComponent<T>();
            return component;
        }

        public static void Add<T1, T2>(this IList<Tuple<T1, T2>> list, T1 item1, T2 item2) {
            list.Add(Tuple.Create(item1, item2));
        }

        public static void Add<T1, T2, T3>(this IList<Tuple<T1, T2, T3>> list, T1 item1, T2 item2, T3 item3) {
            list.Add(Tuple.Create(item1, item2, item3));
        }

         public static void Add(this IList<TutorialStageInfo> list,
                                string a, string b, string c) {
             list.Add(new TutorialStageInfo(a, b, c));
        }

        public static void Add(this IList<TutorialStageInfo> list,
                               string a, string b, string c, TutorialRequirement requirement) {
            list.Add(new TutorialStageInfo(a, b, c, requirement));
        }

        public static EffectSpawner FindEffect(this Component component, EffectType type) {
            var effects = component.GetComponents<EffectSpawner>();
            foreach (var effect in effects) {
                if (effect.effectType == type) {
                    return effect;
                }
            }
            return null;
        }
    }
}


public class Utility {
    // Class for any static utility functions
    public static IntCallback ModCycle(int start, int modulus) {
        int value = start;
        return () => {
            int result = value;
            value = (value + 1) % modulus;
            return result;
        };
    }

    public static Color ColorComplement(Color baseColor) {
        float h;
        float s;
        float v;
        Color.RGBToHSV(baseColor, out h, out s, out v);
        float shiftedH = h + 0.5f;
        h = shiftedH - (shiftedH > 1 ? 1 : 0);
        return Color.HSVToRGB(h, s, v);
    }

    public static void Toggle(GameObject gameObj) {
        gameObj.SetActive(!gameObj.activeInHierarchy);
    }

    public static void Toggle(MonoBehaviour component) {
        component.enabled = !component.enabled;
    }

    public static string UniqueString() {
        return System.Guid.NewGuid().ToString();
    }

    public static void Blowback(Vector2 center, float radius,
                                float blowback_strength, bool blowback_is_velocity = false,
                                int layerMask = Physics2D.DefaultRaycastLayers,
                                GameObjectCallback onCollided = null,
                                HashSet<GameObject> excludes = null) {
        // Usage
        //
        // When `blowback_is_velocity` is false, the `blowback` parameter
        // will be added as a force. When it's true, the force required to reach
        // a velocity given by `blowback` based on collided objects mass will be
        // used.
        //
        // onCollided will be called on each collided object.
        if (onCollided == null) {
            onCollided = delegate{};
        }
        if (excludes == null) {
            excludes = new HashSet<GameObject>();
        }
        var collided = Physics2D.OverlapCircleAll(center, radius, layerMask);
        foreach (var collider in collided) {
            var thing = collider.gameObject;
            if (excludes.Contains(thing)) {
                continue;
            }
            var rigidbody = thing.GetComponent<Rigidbody2D>();
            if (rigidbody != null) {
                onCollided(thing);
                var direction = (Vector2) rigidbody.transform.position - center;
                var knockback = direction * blowback_strength;
                if (blowback_is_velocity) {
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
                                       float? stunTime = null) {
        GameObjectCallback stunPlayer = (GameObject thing) => {
            var player = thing.GetComponent<PlayerStateManager>();
            var stun = thing.GetComponent<PlayerStun>();
            if (player != null) {
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
                                          float? stunTime = null) {
        var ignoreList = player.GetComponent<Player>().team.teamMembers;
        var ignoreSet = new HashSet<GameObject>();
        foreach (Player ignore in ignoreList) {
            ignoreSet.Add(ignore.gameObject);
        }
        BlowbackPlayers(player.transform.position, radius,
                        blowback_strength, blowback_is_velocity,
                        ignoreSet,
                        stunTime);
    }

    public static void TutEvent(string baseName, MonoBehaviour thing) {
        GameModel.instance.nc.NotifyStringEvent(baseName + "Tutorial", thing.gameObject);
    }

    public static void Print(params object[] args) {
        string formatString = "";
        for (int i = 0; i < args.Length; i++) {
            formatString += string.Format("{{{0}}} ", i);
        }
        Debug.LogFormat(formatString, args);
    }

    public static void ChangeTimeScale(float factor) {
        Time.timeScale = 1 * factor;
        Time.fixedDeltaTime = 0.02f * factor;
    }
}

public class ModCycle {
    public int nextValue = 0;
    public int modulus;

    public ModCycle(int start, int modulus) {
        nextValue = start;
        this.modulus = modulus;
    }

    public int PeekNext() {
        return nextValue;
    }

    public int Next() {
        var result = nextValue;
        nextValue = (nextValue + 1) % modulus;
        return result;
    }

}


public class CoroutineUtility : MonoBehaviour {
    // Class for utility functions involving Coroutines.
    //
    // Any functions that simply return IEnumerators/don't call StartCoroutine
    // should be static. Functions that do call StartCoroutine need to be
    // non-static and called through the singleton instance.

    public static CoroutineUtility instance;

    void Awake() {
        if (instance == null) {
            instance = this;
        } else {
            Destroy(gameObject);
        }
    }

    public static IEnumerator RunThenCallback(IEnumerator coroutine, Callback callback) {
        yield return coroutine;
        callback();
    }

    public static IEnumerator WaitForFrames(int frames = 1) {
        for (int i = 0; i < frames; i++) {
            yield return null;
        }
    }

    public static IEnumerator WaitForFixedUpdates(int updates = 1) {
        for (int i = 0; i < updates; i++) {
            yield return new WaitForFixedUpdate();
        }
    }

    // A version of WaitForSeconds that returns an IEnumerator rather than the
    // custom class instance
    public static IEnumerator WaitForSeconds(float seconds) {
        yield return new WaitForSeconds(seconds);
    }

}
