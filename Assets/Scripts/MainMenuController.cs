using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;
using UnityEngine.UI;

using IC = InControl;
using InputManager = InControl.InputManager;

public class MainMenuController : MonoBehaviour {

    IC.InputControlType SelectButton = IC.InputControlType.Action3; // X
    public Color selectedColor;
    public Color deselectedColor;

    public List<Text> MenuOptions = new List<Text>();
    int selectionIndex = 0;
    bool selected;

    // Update is called once per frame
    void Update () {
        if (selected) {
            return;
        }
        foreach (var device in InputManager.Devices) {
            if (device.GetControl(SelectButton).WasPressed) {
                var selection = MenuOptions[selectionIndex];

                AudioManager.instance.ConfirmSelectionSound.Play();

                this.TimeDelayCall(
                                   () => {
                                       var trigger = selection.gameObject.GetComponent<SceneLoadTrigger>();
                                       trigger?.LoadAssociatedScene();
                                   },
                                   AudioManager.instance.ConfirmSelectionSound.Length()
                                   );
                selected = true;
                break;
            } else if (device.GetControl(IC.InputControlType.LeftBumper).WasPressed) {
                GameModel.cheatForcePlayerAssignment = true;
                AudioManager.instance.CheatCodeSound.Play();
                this.TimeDelayCall(
                    () => SceneStateController.instance.Load(Scene.Court),
                    AudioManager.instance.CheatCodeSound.Length());
            }
        }
    }
}
