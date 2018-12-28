using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;
using System.Collections.Generic;
using UtilityExtensions;

// The LobbyManager effectively replaces previous "tutorial" classes
// (specifically PlayerTutorial, not TutorialLiveClips), in their scene
// transition responsibilities.
public class LobbyManager : MonoBehaviourPunCallbacks {
    void Awake() {
        if (!GameManager.playerTeamsAlreadySelected) {
            // This variable should only prevent the start countdown from
            // running, at the moment
            PlayerTutorial.runTutorial = true;
            // This variable should already default to false and never be set to
            // true in the current flow, but keeping it just in case.
            // TODO clean up this variable
            GameManager.cheatForcePlayerAssignment = false;
            // This variable is actually relevant. It defaults to false, but
            // setting it again here ensures reloads of the lobby stay correct.
            // There should actually be no difference between this variable and
            // GameManager.playerTeamsAlreadySelected, logically, but they're
            // already used in different places.
            // TOOD Clean up this variable/merge with playerTeamsAlreadySelected
            TeamManager.playerSpritesAlreadySet = false;
            // Should be obvious, enables the JITT in the court. Although that's
            // non-functional at the moment I think.
            JustInTimeTutorial.alreadySeen = false;
        }
    }
    void Start() {
        GameManager.NotificationManager.CallOnMessage(Message.TeamsChanged, HandleTeamsChanged);
    }

    void HandleTeamsChanged() {
        if (GameManager.Instance.Teams.All(team => team.teamMembers.Count == 2)) {
            EndTeamSelection();
        }
    }

    void EndTeamSelection() {
        if (!GameManager.playerTeamsAlreadySelected) {
            // Set the static variable which tells the game as a whole to use
            // various static data structures to determine player teams.
            GameManager.playerTeamsAlreadySelected = true;
            // Fill in that static structure (to persist over scene loads) with
            // the current TeamManager object information
            GameManager.playerTeamAssignments = new Dictionary<int, int>();
            foreach (Player player in GameManager.Instance.GetPlayersWithTeams())
            {
                int teamIndex = GameManager.Instance.Teams.IndexOf(player.Team);
                GameManager.playerTeamAssignments[player.playerNumber] = teamIndex;
            }
            // Basically the same semantics as playerTeamsAlreadySelected but
            // used in TeamManager instead of elsewhere
            TeamManager.playerSpritesAlreadySet = true;
            // Call the scene load methods. This won't actually load the scene
            // on any non-master client (the auto scene syncing takes care of
            // final loading).
            SceneStateManager.instance.Load(Scene.Court);
        }
    }
}
