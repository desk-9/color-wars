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
    Selection
};

public class SceneStateController : MonoBehaviour {

    Dictionary<Scene, string> scenes = new Dictionary<Scene, string> {
        {Scene.Court, "court"},
        {Scene.MainMenu, "main-menu"},
        {Scene.Selection, "court-team-selection"},
        {Scene.Tutorial, "court-tutorial"},
    };
    public Scene currentScene {get; private set;}

    public Dictionary<Scene, Callback> OnExit = new Dictionary<Scene, Callback>();
    public Dictionary<Scene, Callback> OnEnter = new Dictionary<Scene, Callback>();

    public bool paused {get; private set;} = false;
    float sceneTransitionFadeLength = 2f;

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
        StartCoroutine(ScreenOpacityTransition(1, 0));
    }

    GameObject MakeNewPanel() {
        var newCanvasObj = new GameObject("Canvas");
        Canvas newCanvas = newCanvasObj.AddComponent<Canvas>();
        newCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        newCanvasObj.AddComponent<CanvasScaler>();
        newCanvasObj.AddComponent<GraphicRaycaster>();
        GameObject panel = new GameObject("Panel");
        panel.AddComponent<CanvasRenderer>();
        panel.transform.SetParent(newCanvasObj.transform, false);
        var rectTransform = panel.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        return panel;
    }

    // Ripped  from
    // https://answers.unity.com/questions/1034060/create-unity-ui-panel-via-script.html
    IEnumerator ScreenOpacityTransition(float opacityStart, float opacityEnd) {
        var newPanel = MakeNewPanel();
        Image panelImage = newPanel.AddComponent<Image>();
        var color = new Color(0,0,0,0);
        panelImage.color = color;

        var elapsedTime = 0f;
        var lastTime = Time.realtimeSinceStartup;
        while (elapsedTime < sceneTransitionFadeLength) {
            color.a = Mathf.Lerp(opacityStart, opacityEnd, elapsedTime/sceneTransitionFadeLength);
            panelImage.color = color;
            elapsedTime += Time.realtimeSinceStartup - lastTime;
            yield return null;
        }
        color.a = 0f;
        panelImage.color = color; 
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
        StartCoroutine(CoroutineUtility.RunThenCallback(ScreenOpacityTransition(0, 1), () => LoadHelper(newScene)));
    }

    public void ReloadScene() {
        StartCoroutine(CoroutineUtility.RunThenCallback(ScreenOpacityTransition(0, 1), () =>
                                                        SceneManager.LoadScene(SceneManager.GetActiveScene().name)));
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
