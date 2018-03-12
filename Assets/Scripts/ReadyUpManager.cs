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
        startGameCountdownText.text = "About to start game...\n3";
        Debug.Log("Waiting 1");
        yield return new WaitForSeconds(1);
        
        startGameCountdownText.text = "About to start game...\n2";
        Debug.Log("Waiting 2");
        yield return new WaitForSeconds(1);
        
        startGameCountdownText.text = "About to start game...\n1";
        Debug.Log("Waiting 3");
        yield return new WaitForSeconds(1);
        
        startGameCountdownText.text = "About to start game...\nGO!";
        yield return new WaitForSeconds(1);
        menuController.enabled = true;
        Debug.Log("Loading main scene!!");
        SceneManager.LoadScene(mainScene, LoadSceneMode.Single);
    }

    void UpdateText() {
        readyUpText.text = "Ready: " + numPlayersReady.ToString()
            + "/" + numPlayers.ToString();
    }

}
