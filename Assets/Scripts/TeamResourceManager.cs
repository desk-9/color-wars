using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class TeamResourceManager {

    public GameObject dashEffectPrefab {get; private set;}
    public GameObject dashChargeEffectPrefab {get; private set;}
    public GameObject shootChargeEffectPrefab {get; private set;}
    public GameObject scoreGoalEffectPrefab {get; private set;}
    public Sprite mainPlayerSprite {get; private set;}
    public Sprite altPlayerSprite {get; private set;}
    public Material wallMaterial {get; private set;}
    public GameObject dashAimerPrefab {get; private set;}
    public Sprite ballSprite {get; private set;}
    public GameObject explosionPrefab {get; private set;}
    public GameObject tronWallDestroyedPrefab {get; private set;}
    public GameObject tronWallSuicidePrefab {get; private set;}
    public Sprite background {get; private set;}
    public Sprite scoreIndicatorEmptySprite {get; private set;}
    public Sprite scoreIndicatorFullSprite {get; private set;}

    public Color teamColor = Color.white;
    public Gradient aimLaserGradient = null;
    public Gradient aimLaserToGoalGradient = null;

    TeamManager team;
    string teamDirectory = "Teams/Neutral";
    // The directory for resources all teams use
    const string allTeamDirectory = "Teams/All";
    public TeamResourceManager(TeamManager team) {
        this.team = team;
        if (team != null) {
            teamDirectory = string.Format("Teams/{0}", team.teamColor.name);
        }
        SetupResources();
    }

    T LoadTeamResource<T>(string name, string teamDirectory) where T : UnityEngine.Object {
        var path = string.Format("{0}/{1}", teamDirectory, name);
        var resource = Resources.Load<T>(path);
        if (resource == null) {
            throw new ArgumentException(
                string.Format("No resource of type {0} at {1}",
                              typeof(T), path));
        }
        return resource;
    }

    T MakeTeamResource<T>(string name) where T : UnityEngine.Object {
        // For making resources specific to this team
        return LoadTeamResource<T>(name, teamDirectory);
    }

    List<T> MakeTeamResourceGroup<T>(params string[] names) where T : UnityEngine.Object {
        return (from name in names
                select LoadTeamResource<T>(name, teamDirectory)).ToList();
    }

    T MakeAllTeamResource<T>(string name) where T : UnityEngine.Object {
        // For making resources shared by all teams
        return LoadTeamResource<T>(name, allTeamDirectory);
    }

    void SetupResources() {
        // Pass in teamDirectory as the second argument for team-specific
        // resources, and allTeamDirectory for resources shared by all teams
        dashEffectPrefab = MakeTeamResource<GameObject>("DashEffect");
        dashChargeEffectPrefab = MakeTeamResource<GameObject>("DashChargeEffect");
        wallMaterial = MakeTeamResource<Material>("Wall");
        ballSprite = MakeTeamResource<Sprite>("Ball");
        dashAimerPrefab = MakeTeamResource<GameObject>("DashAimer");
        background = MakeTeamResource<Sprite>("Background");
        scoreGoalEffectPrefab = MakeTeamResource<GameObject>("ScoreGoalEffect");

        mainPlayerSprite = MakeAllTeamResource<Sprite>("MainPlayer");
        altPlayerSprite = MakeAllTeamResource<Sprite>("AltPlayer");
        shootChargeEffectPrefab = MakeAllTeamResource<GameObject>("ShootChargeEffect");
        explosionPrefab = MakeAllTeamResource<GameObject>("ExplosionPrefab");
        tronWallDestroyedPrefab = MakeAllTeamResource<GameObject>("TronWallDestroyed");
        tronWallSuicidePrefab = MakeAllTeamResource<GameObject>("TronWallSuicide");
        scoreIndicatorEmptySprite = MakeAllTeamResource<Sprite>("ScoreIndicatorEmpty");
        scoreIndicatorFullSprite = MakeAllTeamResource<Sprite>("ScoreIndicatorFull");

        teamColor = (team == null)? Color.white : this.team.teamColor.color;

        aimLaserGradient = aimLaserGradient != null?
            aimLaserGradient : TeamAimLaserGradient();
        
        aimLaserToGoalGradient = aimLaserToGoalGradient != null?
            aimLaserToGoalGradient : TeamAimLaserGradient(false);
    }

    Gradient TeamAimLaserGradient(bool fade = true) {
        float alpha = fade? 0.0f : 1.0f;
        var gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(teamColor, 0.0f),
                new GradientColorKey(teamColor, 1.0f) },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(1.0f, 0.8f),
                new GradientAlphaKey(alpha, 1.0f) }
            );
        return gradient;
    }
    
}
