using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour {

    public float colorFlashLength;
    public float colorFlashFadeTime;
	public TeamManager team {get; private set;}
	
    new SpriteRenderer renderer;
    Coroutine flashColorCoroutine;

    public void FlashTeamColor()
    {
        // Prevents repeated calling
        if (flashColorCoroutine == null) {
            flashColorCoroutine = StartCoroutine(FlashColorCoroutine());
        }
    }

    IEnumerator FlashColorCoroutine()
    {
        Color originalColor = renderer.color;

        renderer.color = team.teamColor;
        yield return new WaitForSeconds(colorFlashLength);

        float elapsedTime = 0f;
        while (elapsedTime < colorFlashFadeTime) {
            elapsedTime += Time.deltaTime;
            renderer.color = Color.Lerp(team.teamColor, originalColor, elapsedTime / colorFlashFadeTime);
            yield return null;
        }
        flashColorCoroutine = null;
    }

	// Use this for initialization
	void Start () {
        renderer = GetComponent<SpriteRenderer>();
        team = GameModel.instance.GetTeamAssignment(this);
        Debug.LogFormat("Assigned player {0} to team {1}", name, team.teamNumber);
	}
}
