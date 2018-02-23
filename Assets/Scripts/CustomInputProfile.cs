using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InControl;

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
        };
    }
}
