using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum Scene {
    Court,
    MainMenu,
    Tutorial,
};

public class SceneStateController : MonoBehaviour {

    Dictionary<Scene, string> scenes = new Dictionary<Scene, string> {
        {Scene.Court, "court"},
        {Scene.MainMenu, "main-menu"},
        {Scene.Tutorial, "court"},
    };
    public Scene currentScene {get; private set;}

    public Dictionary<Scene, Callback> OnExit = new Dictionary<Scene, Callback>();
    public Dictionary<Scene, Callback> OnEnter = new Dictionary<Scene, Callback>();

    public bool paused {get; private set;} = false;

    public static SceneStateController instance;
    void Awake() {
        if (instance == null) {
            instance = this;
            InitializeCallbacks();
        }
        else {
            Destroy(gameObject);
        }
    }

    void InitializeCallbacks() {
        // DontDestroyOnLoad(this.gameObject);
        foreach (Scene scene in System.Enum.GetValues(typeof(Scene))) {
            OnEnter[scene] = delegate{};
            OnExit[scene] = delegate{};
        }
    }

    public void Load(Scene newScene) {
        OnExit[currentScene]();
        currentScene = newScene;
        SceneManager.LoadScene(scenes[currentScene]);
        AdjustTime(newScene);
        OnEnter[currentScene]();

        UnPauseTime();
    }

    public void ReloadScene() {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void AdjustTime(Scene newScene) {
        switch (newScene) {
        case Scene.Court:
            UnPauseTime();
            break;
        case Scene.MainMenu:
            PauseTime();
            break;
        case Scene.Tutorial:
            UnPauseTime();
            break;
        default:
            break;
        }
    }

    public void TogglePauseTime() {
        if (paused) {
            UnPauseTime();
        } else {
            PauseTime();
        }
    }

    public void PauseTime() {
        Time.timeScale = 0.0f;
        paused = true;
    }

    public void UnPauseTime() {
        Time.timeScale = 1.0f;
        paused = false;
    }


}
