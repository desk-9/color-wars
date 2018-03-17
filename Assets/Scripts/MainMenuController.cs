using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;
using UnityEngine.UI;

using IC = InControl;
using InputManager = InControl.InputManager;

public class MainMenuController : MonoBehaviour {

    public IC.InputControlType SelectButton = IC.InputControlType.Action1; // A
    public Color selectedColor;
    public Color deselectedColor;

    public List<Text> MenuOptions = new List<Text>();
    int selectionIndex = 0;
    PlayerInputManager playerInputManager;

    void Start() {
        playerInputManager = PlayerInputManager.instance;
    }
	
    // Update is called once per frame
    void Update () {
        foreach (var device in InputManager.Devices) {

            // Change selection?
            Vector2 direction = playerInputManager.GetLeftStickInput(device);
            if (direction.x > 0) {
                IncrementSelection();
            }
            else if (direction.x < 0) {
                DecrementSelection();
            }

            // Load selected scene?
            else if (device.GetControl(SelectButton).WasPressed) {
                var selection = MenuOptions[selectionIndex];
                var trigger = selection.gameObject.GetComponent<SceneLoadTrigger>();
                trigger?.LoadAssociatedScene();
            }
        }
    }

    void IncrementSelection() {
        // Can't increment -- we're at the last element
        if (selectionIndex == MenuOptions.Count-1) {
            return;
        }
        MenuOptions[selectionIndex].color = deselectedColor;
        selectionIndex += 1;
        MenuOptions[selectionIndex].color = selectedColor;
    }

    void DecrementSelection() {
        if (selectionIndex == 0) {
            return;
        }
        MenuOptions[selectionIndex].color = deselectedColor;
        selectionIndex -= 1;
        MenuOptions[selectionIndex].color = selectedColor;
    }
    
    
}
