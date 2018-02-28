using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour {

    public float colorFlashLength;
    public float colorFlashFadeSpeed;

    TeamManager team;
    SpriteRenderer rend;
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
        Color originalColor = rend.color;

        rend.color = team.teamColor;
        yield return new WaitForSeconds(colorFlashLength);

        while(rend.color != originalColor) {
            rend.color = Color.Lerp(rend.color, originalColor, colorFlashFadeSpeed * Time.deltaTime);
            yield return null;
        }
        flashColorCoroutine = null;
    }

	// Use this for initialization
	void Start () {
        rend = GetComponent<SpriteRenderer>();
        team = GameModel.instance.GetTeamAssignment(this);
        Debug.LogFormat("Assigned player {0} to team {1}", name, team.teamNumber);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
