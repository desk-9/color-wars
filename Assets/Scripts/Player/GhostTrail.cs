using System.Collections;
using UnityEngine;
using UtilityExtensions;

public class GhostTrail : MonoBehaviour
{

    public float ghostLifeLength = .2f;
    public GameObject ghostObject;
    public float timeBetweenGhosts;
    public float startingAlpha;
    private new SpriteRenderer renderer;
    private Coroutine ghostCoroutine;

    // Use this for initialization
    private void Start()
    {
        if (this == null)
        {
            return;
        }
        renderer = this.EnsureComponent<SpriteRenderer>();
        NotificationManager notificationManager = GameManager.NotificationManager;
        notificationManager.CallOnMessage(Message.BallIsPossessed, StartGhost);
        notificationManager.CallOnMessage(Message.BallIsUnpossessed, StopGhost);
    }

    private void StartGhost()
    {
        if (this == null)
        {
            return;
        }
        if (ghostCoroutine == null)
        {
            ghostCoroutine = StartCoroutine(AddGhostObject());
        }
    }

    private void StopGhost()
    {
        if (this == null)
        {
            return;
        }
        if (ghostCoroutine != null)
        {
            StopCoroutine(ghostCoroutine);
            ghostCoroutine = null;
        }
    }

    private IEnumerator AddGhostObject()
    {
        while (true)
        {
            // TODO dkonik: Pool these
            GameObject newGhostObject = GameObject.Instantiate(ghostObject, transform.position, transform.rotation);
            newGhostObject.transform.localScale = transform.localScale;
            newGhostObject.GetComponent<Ghost>().Initialize(renderer, ghostLifeLength, startingAlpha);
            yield return null;
        }
    }
}
