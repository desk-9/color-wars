using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InControl;

// This is a profile used by InControl that allows us to map buttons to other
// buttons. Namely, it allows us to map WASD and the arrow keys to the LeftStick,
// so that in the code we only have to check on thing, rather than three.
public class CustomPlayerProfile : UnityInputDeviceProfile
{

    public CustomPlayerProfile()
    {
        Name = "CustomPlayerProfile";
        Meta = "Allows both arrow keys and joystick";

        SupportedPlatforms = new[]
        {
            "Windows",
            "Mac",
            "Linux"
        };

        // Not sure what these do, but they were in the example
        Sensitivity = 1.0f;
        LowerDeadZone = 0.0f;
        UpperDeadZone = 1.0f;

        AnalogMappings = new[]
        {
            new InputControlMapping {
                Handle = "Move X",
                Target = InputControlType.LeftStickX,
                Source = KeyCodeAxis( KeyCode.A, KeyCode.D )
            },
            new InputControlMapping {
                Handle = "Move X Alternate",
                Target = InputControlType.LeftStickX,
                Source = KeyCodeAxis( KeyCode.LeftArrow, KeyCode.RightArrow )
            },
            new InputControlMapping {
                Handle = "Move Y",
                Target = InputControlType.LeftStickY,
                Source = KeyCodeAxis( KeyCode.S, KeyCode.W )
            },
            new InputControlMapping {
                Handle = "Move Y Alternate",
                Target = InputControlType.LeftStickY,
                Source = KeyCodeAxis( KeyCode.DownArrow, KeyCode.UpArrow )
            },
            new InputControlMapping {
                Handle = "Dash",
                Target = InputControlType.Action2,
                Source = KeyCodeButton( KeyCode.X )
            }
        };
    }
}
