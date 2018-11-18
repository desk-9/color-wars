using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UtilityExtensions;

public class ScoreIndicator : MonoBehaviour
{

    public string teamName;
    public List<Color> stops;
    public List<float> durations;
    private TeamManager team = null;
    private List<GameObject> pointIndicators = new List<GameObject>();

    private void Start()
    {
        this.FrameDelayCall(Initialization, 2);
    }

    // Use this for initialization
    private void Initialization()
    {

        // Find team
        foreach (TeamManager candidateTeam in GameModel.instance.teams)
        {
            if (candidateTeam.teamColor.name == teamName)
            {
                team = candidateTeam;
                break;
            }
        }
        if (team == null)
        {
            Debug.LogError("Could not associate team to ScoreIndicator!");
            Destroy(this);
        }

        // Set last lerp color to the team color
        stops[stops.Count - 1] = team.teamColor.color;

        // Find references to child indicator GameObjects
        foreach (Transform childIndicator in
                 transform.Cast<Transform>().OrderBy(t => t.name))
        {
            pointIndicators.Add(childIndicator.gameObject);
        }

        // Update score indicator when a goal is scored
        GameModel.instance.notificationCenter.CallOnMessageWithSender(
            Message.GoalScored,
            (object scoringTeam) =>
            {
                if ((TeamManager)scoringTeam == team)
                {
                    DisplayNextPoint();
                }
            });

        // Reset score indicator when game is restarted
        GameModel.instance.notificationCenter.CallOnMessage(Message.ScoreChanged, UpdateAllDisplays);

        foreach (GameObject pointIndicator in pointIndicators)
        {
            pointIndicator.GetComponent<SpriteRenderer>().color = team.teamColor.color;
        }
    }

    public void UpdateAllDisplays()
    {
        for (int i = 0; i < pointIndicators.Count; ++i)
        {
            SpriteRenderer renderer = pointIndicators[i].GetComponent<SpriteRenderer>();
            renderer.sprite = (i < team.score) ?
                team.resources.scoreIndicatorFullSprite :
                team.resources.scoreIndicatorEmptySprite;
            renderer.color = team.teamColor.color;
        }
    }

    public void DisplayNextPoint()
    {
        // Scores are 1-indexed, pointIndicators are 0-indexed
        // ASSUMPTION: this function is invoked *after* team.score has been updated
        int nextPoint = team.score - 1;
        GameObject pointIndicator = pointIndicators[nextPoint];
        SpriteRenderer renderer = pointIndicator.GetComponent<SpriteRenderer>();

        renderer.sprite = team.resources.scoreIndicatorFullSprite;
        StartCoroutine(TransitionUtility.LerpColorSequence(
            (Color color) => renderer.color = color,
            stops, durations));
        StartParticleEffect(pointIndicator);
    }

    private void StartParticleEffect(GameObject pointIndicator)
    {
        // Start particle effect
        GameObject scoreGoalEffect = GameObject.Instantiate(
            team.resources.scoreGoalEffectPrefab,
            pointIndicator.gameObject.transform.position,
            pointIndicator.gameObject.transform.rotation);
        ParticleSystem scoreGoalParticleSystem = scoreGoalEffect.EnsureComponent<ParticleSystem>();
        ParticleSystem.MainModule scoreGoalMain = scoreGoalParticleSystem.main;
        scoreGoalMain.startColor = team.teamColor.color;
        scoreGoalParticleSystem.Play();
        this.TimeDelayCall(() => Destroy(scoreGoalEffect), scoreGoalMain.duration);
    }

}
