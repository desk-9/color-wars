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
}

public class ModCycle {
    public int value = 0;
    public int modulus;

    public ModCycle(int start, int modulus) {
        value = start;
        this.modulus = modulus;
    }

    public int PeekNext() {
        return value;
    }

    public int Next() {
        var result = value;
        value = (value + 1) % modulus;
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
