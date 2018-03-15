using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;

public class ReadyUpManager : MonoBehaviour {

    public Text readyUpText;
    public Text startGameCountdownText;
    public Text controlsText;

    public int numPlayers {
        get { return players.Count; }
        private set { numPlayers = value; }
    }
    public int numPlayersReady {
        get { return readyPlayers.Count; }
        private set { numPlayersReady = value; }
    }


    List<PlayerTutorial> players;
    List<PlayerTutorial> readyPlayers;
    string mainScene = "court";
    MenuController menuController;

    // Singleton
    public static ReadyUpManager instance;
    void Awake() {
        if (instance == null) {
            instance = this;
            players = new List<PlayerTutorial>();
            readyPlayers = new List<PlayerTutorial>();
        }
        else {
            Destroy(gameObject);
        }
    }

    void Start() {
        if (readyUpText == null) {
            Debug.LogWarning("Missing reference: readyUpText!");
        }
        if (startGameCountdownText == null) {
            Debug.LogWarning("Missing reference: readyUpText!");
        }
        if (controlsText == null) {
            Debug.LogWarning("Missing reference: controlsText!");
        }

        startGameCountdownText.text = "";
        menuController = GetComponent<MenuController>();
        menuController.enabled = false;
    }

    public void RegisterPlayer(PlayerTutorial player) {
        if (players.Contains(player)) {return;}
        players.Add(player);
        UpdateText();
    }

    public void RegisterReadyPlayer(PlayerTutorial player) {
        if (readyPlayers.Contains(player)) {return;}
        readyPlayers.Add(player);
        UpdateText();

        if ((numPlayersReady == numPlayers) && (numPlayers > 0)) {
            StartCoroutine(StartGameText());
        }
    }

    IEnumerator StartGameText() {
        controlsText.text = "";
        for (int i = 3; i > 0; i--) {
            startGameCountdownText.text = string.Format("Starting game...\n{0}", i);
            Debug.LogFormat("Waiting {0}", 4 - i);
            yield return new WaitForSeconds(1);
        }

        startGameCountdownText.text = "Starting game...\nGO!";
        yield return new WaitForSeconds(1);
        Debug.Log("Loading main scene!!");
        SceneManager.LoadScene(mainScene, LoadSceneMode.Single);
    }

    void UpdateText() {
        readyUpText.text = string.Format("Ready: {0}/{1}", numPlayersReady, numPlayers);
    }

}
