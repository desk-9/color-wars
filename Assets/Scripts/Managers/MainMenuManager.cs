using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;
using UnityEngine.UI;

using IC = InControl;
using InputManager = InControl.InputManager;

public class MainMenuManager : MonoBehaviour
{
    private IC.InputControlType SelectButton = IC.InputControlType.Action3; // X
    public Color selectedColor;
    public Color deselectedColor;

    public List<Text> MenuOptions = new List<Text>();
    private int selectionIndex = 0;
    private bool selected;

    // Update is called once per frame
    private void Update()
    {
        if (selected)
        {
            return;
        }
        foreach (IC.InputDevice device in InputManager.Devices)
        {
            if (device.GetControl(SelectButton).WasPressed)
            {
                Text selection = MenuOptions[selectionIndex];

                AudioManager.instance.ConfirmSelectionSound.Play();

                this.TimeDelayCall(
                                   () =>
                                   {
                                       SceneStateManager.instance.Load(Scene.Selection);
                                   },
                                   AudioManager.instance.ConfirmSelectionSound.Length()
                                   );
                selected = true;
                break;
            }
            else if (device.GetControl(IC.InputControlType.LeftBumper).WasPressed)
            {
                GameManager.cheatForcePlayerAssignment = true;
                AudioManager.instance.CheatCodeSound.Play();
                this.TimeDelayCall(
                    () => SceneStateManager.instance.Load(Scene.Court),
                    AudioManager.instance.CheatCodeSound.Length());
            }
        }
    }
}
