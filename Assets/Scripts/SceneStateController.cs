using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UtilityExtensions;

public enum Scene {
    Court,
    MainMenu,
    Tutorial,
    Selection,
    Sandbox
};

public class SceneStateController : MonoBehaviour {

    Dictionary<Scene, string> scenes = new Dictionary<Scene, string> {
        {Scene.Court, "court"},
        {Scene.MainMenu, "main-menu"},
        {Scene.Selection, "court-team-selection"},
        {Scene.Tutorial, "court-tutorial"},
        {Scene.Sandbox, "court-sandbox"},
    };
    public Scene currentScene {get; private set;}

    public Dictionary<Scene, Callback> OnExit = new Dictionary<Scene, Callback>();
    public Dictionary<Scene, Callback> OnEnter = new Dictionary<Scene, Callback>();

    public bool paused {get; private set;} = false;
    float sceneTransitionDuration = 0.5f;
    TransitionUtility.ScreenTransition screenTransition;

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

    void Start() {
        screenTransition = new TransitionUtility.ScreenTransition(
            sceneTransitionDuration);
        StartCoroutine(screenTransition.FadeIn());

        // Disable mouse.
        Cursor.visible = false;
    }

    void InitializeCallbacks() {
        foreach (Scene scene in System.Enum.GetValues(typeof(Scene))) {
            OnEnter[scene] = delegate{};
            OnExit[scene] = delegate{};
        }
    }

    void LoadHelper(Scene newScene) {
        OnExit[currentScene]();
        currentScene = newScene;
        if (newScene == Scene.Selection) {
            PlayerTutorial.runTutorial = newScene == Scene.Selection;
            GameModel.playerTeamsAlreadySelected = false;
            GameModel.cheatForcePlayerAssignment = false;
            TeamManager.playerSpritesAlreadySet = false;
        }
        SceneManager.LoadScene(scenes[currentScene]);
        AdjustTime(newScene);
        OnEnter[currentScene]();

        UnPauseTime();
    }

    public void Load(Scene newScene) {
        StartCoroutine(CoroutineUtility.RunThenCallback(
            screenTransition.FadeOut(),
            () => LoadHelper(newScene)));
    }

    public void ReloadScene() {
        StartCoroutine(CoroutineUtility.RunThenCallback(
            screenTransition.FadeOut(),
            () => SceneManager.LoadScene(SceneManager.GetActiveScene().name)));
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
