using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UtilityExtensions;

public class ScoreIndicator : MonoBehaviour {

    public string teamName;
    public float colorLerpDuration = 0.75f;
    public Color startLerpColor = Color.black;
    
    TeamManager team = null;
    List<GameObject> pointIndicators = new List<GameObject>();
    

    void Start() {
        this.FrameDelayCall(Initialization, 2);
    }
    
    // Use this for initialization
    void Initialization () {

        // Find team
        foreach (var candidateTeam in GameModel.instance.teams) {
            if (candidateTeam.teamColor.name == teamName) {
                team = candidateTeam;
                break;
            }
        }
        if (team == null) {
            Debug.LogError("Could not associate team to ScoreIndicator!");
            Destroy(this);
        }

        // Find references to child indicator GameObjects
        foreach (Transform childIndicator in
                 transform.Cast<Transform>().OrderBy(t=>t.name)) {
            pointIndicators.Add(childIndicator.gameObject);
        }

        // Update score indicator when a goal is scored
        GameModel.instance.nc.CallOnMessageWithSender(
            Message.GoalScored,
            (object scoringTeam) => {
                if ((TeamManager)scoringTeam == team) {
                    DisplayNextPoint();
                }
            });

        // Reset score indicator when game is restarted
        GameModel.instance.nc.CallOnMessage(Message.GoalScored, UpdateAllDisplays);

        foreach (var pointIndicator in pointIndicators) {
            pointIndicator.GetComponent<SpriteRenderer>().color = team.teamColor.color;
        }
    }

    public void UpdateAllDisplays() {
        for (int i = 0; i < pointIndicators.Count; ++i) {
            var renderer = pointIndicators[i].GetComponent<SpriteRenderer>();
                renderer.sprite = (i < team.score)?
                    team.resources.scoreIndicatorFullSprite :
                    team.resources.scoreIndicatorEmptySprite;
            renderer.color = team.teamColor.color;
        }
    }

    public void DisplayNextPoint() {
        // Scores are 1-indexed, pointIndicators are 0-indexed
        // ASSUMPTION: this function is invoked *after* team.score has been updated
        int nextPoint = team.score - 1;
        var renderer = pointIndicators[nextPoint].GetComponent<SpriteRenderer>();
        renderer.sprite = team.resources.scoreIndicatorFullSprite;
        StartCoroutine(LerpNewPointColor(renderer));
    }

    public IEnumerator LerpNewPointColor(SpriteRenderer renderer) {
        float timeElapsed = 0.0f;
        float progress = 0.0f;
        Color finalColor = team.teamColor.color;
        while (timeElapsed < colorLerpDuration) {
            timeElapsed += Time.deltaTime;
            progress = timeElapsed / colorLerpDuration;
            renderer.color = Color.Lerp(startLerpColor, finalColor, progress);
            yield return null;
        }
        renderer.color = team.teamColor.color;
    }
    
}
