using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UtilityExtensions;

public delegate List<GameObject> CheckinLister();

public class PlayerCheckin
{
    public bool forceCheckinBypass = false;
    private CheckinLister lister;
    private Predicate allCheckedInPredicate = () => true;
    private EventPredicate checkinPredicate = (o => true);
    public Callback onCheckin = delegate { };
    public Callback onReset = delegate { };
    private Dictionary<GameObject, bool> checkin = new Dictionary<GameObject, bool>();
    private bool listening = false;

    public PlayerCheckin(CheckinLister lister, Message checkinEvent,
                         Predicate allCheckedInPredicate = null,
                         EventPredicate checkinPredicate = null,
                         Callback onCheckin = null,
                         Callback onReset = null,
                         Message? checkoutEvent = null)
    {
        this.lister = lister;
        if (allCheckedInPredicate != null)
        {
            this.allCheckedInPredicate = allCheckedInPredicate;
        }
        if (checkinPredicate != null)
        {
            this.checkinPredicate = checkinPredicate;
        }
        if (onCheckin != null)
        {
            this.onCheckin = onCheckin;
        }
        if (onReset != null)
        {
            this.onReset = onReset;
        }
        GameManager.Instance.NotificationManager.CallOnMessageWithSender(checkinEvent, Checkin);
        if (checkoutEvent.HasValue)
        {
            GameManager.Instance.NotificationManager.CallOnMessageWithSender(checkoutEvent.Value, Checkout);
        }
    }

    public static PlayerCheckin TextCountCheckin(
        CheckinLister lister, Message checkinEvent,
        Text textBox, Predicate allCheckedInPredicate = null,
        EventPredicate checkinPredicate = null,
        Callback onCheckin = null,
        Callback onReset = null,
        Message? checkoutEvent = null)
    {

        PlayerCheckin result = new PlayerCheckin(
            lister, checkinEvent, allCheckedInPredicate,
            checkinPredicate, checkoutEvent: checkoutEvent);

        Callback modifyText = () =>
        {
            textBox.text = string.Format("{0}/{1}", result.NumberCheckedIn(), lister().Count);
        };
        onCheckin = onCheckin ?? delegate { };
        onCheckin += modifyText;

        onReset = onReset ?? delegate { };
        onReset += modifyText;

        result.onCheckin = onCheckin;
        result.onReset = onReset;
        return result;
    }

    public void StartListening()
    {
        listening = true;
    }

    public void StopListening()
    {
        listening = false;
    }

    public void Checkin(object potentialPlayer)
    {
        if (!listening)
        {
            return;
        }
        GameObject player = potentialPlayer as GameObject;
        if (player != null && checkinPredicate(player))
        {
            checkin[player] = true;
        }
        onCheckin();
    }

    public void Checkout(object potentialPlayer)
    {
        if (!listening)
        {
            return;
        }
        GameObject player = potentialPlayer as GameObject;
        if (player != null && checkinPredicate(player))
        {
            checkin[player] = false;
        }
        // This is not a typo. This class is in a semantically-half-backed
        // state, and probably shouldn't be used unless your name is Spruce.
        onCheckin();
    }

    public void ResetCheckin()
    {
        forceCheckinBypass = false;
        foreach (GameObject player in lister())
        {
            checkin[player] = false;
        }
        onReset();
    }

    public int NumberCheckedIn()
    {
        return lister().Count(player => checkin.GetDefault(player, false));
    }

    public bool AllCheckedIn()
    {
        bool allPlayers = (from player in lister()
                           select checkin.GetDefault(player, false)).All(x => x);
        return (allPlayers && allCheckedInPredicate()) || forceCheckinBypass;
    }
};
