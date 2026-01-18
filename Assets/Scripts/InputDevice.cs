using System.Diagnostics;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class InputDevice
{
    public virtual bool Up => false;
    public virtual bool Down => false;
    public virtual bool Left => false;
    public virtual bool Right => false;
    public virtual bool Jump => false;
    public virtual bool Dash => false;
    public virtual bool FastFall => false;
    public virtual bool Attack => false;

    private bool _previousUp = false;
    private bool _previousDown = false;
    private bool _previousLeft = false;
    private bool _previousRight = false;
    private bool _previousJump = false;
    private bool _previousDash = false;
    private bool _previousFastFall = false;
    private bool _previousAttack = false;

    public bool UpDown { get; private set; }
    public bool DownDown { get; private set; }
    public bool LeftDown { get; private set; }
    public bool RightDown { get; private set; }
    public bool JumpDown { get; private set; }
    public bool DashDown { get; private set; }
    public bool FastFallDown { get; private set; }
    public bool AttackDown { get; private set; }

    public bool UpUp { get; private set; }
    public bool DownUp { get; private set; }
    public bool LeftUp { get; private set; }
    public bool RightUp { get; private set; }
    public bool JumpUp { get; private set; }
    public bool DashUp { get; private set; }
    public bool FastFallUp { get; private set; }
    public bool AttackUp { get; private set; }

    public bool Any => Up || Down || Left || Right || Jump || Dash || FastFall || Attack;

    public void Update()
    {
        UpDown = Up && !_previousUp;
        DownDown = Down && !_previousDown;
        LeftDown = Left && !_previousLeft;
        RightDown = Right && !_previousRight;

        JumpDown = Jump && !_previousJump;
        DashDown = Dash && !_previousDash;
        FastFallDown = FastFall && !_previousFastFall;
        AttackDown = Attack && !_previousAttack;

        UpUp = !Up && _previousUp;
        DownUp = !Down && _previousDown;
        LeftUp = !Left && _previousLeft;
        RightUp = !Right && _previousRight;

        JumpUp = !Jump && _previousJump;
        DashUp = !Dash && _previousDash;
        FastFallUp = !FastFall && _previousFastFall;
        AttackUp = !Attack && _previousAttack;

        _previousUp = Up;
        _previousDown = Down;
        _previousLeft = Left;
        _previousRight = Right;

        _previousJump = Jump;
        _previousDash = Dash;
        _previousFastFall = FastFall;
        _previousAttack = Attack;
    }
}

public class KeyboardInputDevice : InputDevice
{
    public Keyboard Keyboard { get; private set; }

    public KeyboardInputDevice(Keyboard keyboard)
    {
        Keyboard = keyboard;
    }

    public override bool Up => Keyboard.wKey.isPressed;
    public override bool Down => Keyboard.sKey.isPressed;
    public override bool Left => Keyboard.aKey.isPressed;
    public override bool Right => Keyboard.dKey.isPressed;
    public override bool Jump => Keyboard.spaceKey.isPressed;
    public override bool Dash => Keyboard.rightShiftKey.isPressed;
    public override bool FastFall => Keyboard.sKey.isPressed;
    public override bool Attack => Keyboard.slashKey.isPressed;
}

public class GamepadInputDevice : InputDevice
{
    private const float STICK_THRESHOLD = 0.2f;
    public override bool Up => Gamepad.leftStick.up.ReadValue() > STICK_THRESHOLD;
    public override bool Down => Gamepad.leftStick.down.ReadValue() > STICK_THRESHOLD;
    public override bool Left => Gamepad.leftStick.left.ReadValue() > STICK_THRESHOLD;
    public override bool Right => Gamepad.leftStick.right.ReadValue() > STICK_THRESHOLD;
    public override bool Jump => Gamepad.buttonSouth.isPressed;
    public override bool Dash => Gamepad.leftTrigger.ReadValue() > STICK_THRESHOLD || Gamepad.rightTrigger.ReadValue() > STICK_THRESHOLD;
    public override bool FastFall => Gamepad.leftStick.down.ReadValue() > STICK_THRESHOLD;
    public override bool Attack => Gamepad.buttonWest.isPressed;

    public Gamepad Gamepad { get; private set; }

    public GamepadInputDevice(Gamepad gamepad)
    {
        Gamepad = gamepad;
    }
}

public class JoystickInputDevice : InputDevice
{
    private const float STICK_THRESHOLD = 0.2f;
    public Joystick Joystick { get; private set; }

    public JoystickInputDevice(Joystick joystick)
    {
        Joystick = joystick;
    }

    public override bool Up => Joystick.stick.up.ReadValue() > STICK_THRESHOLD;
    public override bool Down => Joystick.stick.down.ReadValue() > STICK_THRESHOLD;
    public override bool Left => Joystick.stick.left.ReadValue() > STICK_THRESHOLD;
    public override bool Right => Joystick.stick.right.ReadValue() > STICK_THRESHOLD;
    public override bool Jump => (Joystick.allControls[12] as ButtonControl).isPressed;
    public override bool Dash => (Joystick.allControls[18] as AxisControl).ReadValue() > STICK_THRESHOLD || (Joystick.allControls[17] as AxisControl).ReadValue() > STICK_THRESHOLD;
    public override bool FastFall => Joystick.stick.down.ReadValue() > STICK_THRESHOLD;
    public override bool Attack => (Joystick.allControls[14] as ButtonControl).isPressed;
}

public class StickAgentInputDevice : InputDevice
{
    private bool _up;
    private bool _down;
    private bool _left;
    private bool _right;
    private bool _jump;
    private bool _dash;
    private bool _fastFall;
    private bool _attack;

    public void SetState(bool up, bool down, bool left, bool right, bool jump, bool dash, bool fastFall, bool attack)
    {
        _up = up;
        _down = down;
        _left = left;
        _right = right;
        _jump = jump;
        _dash = dash;
        _fastFall = fastFall;
        _attack = attack;
    }

    public override bool Up => _up;
    public override bool Down => _down;
    public override bool Left => _left;
    public override bool Right => _right;
    public override bool Jump => _jump;
    public override bool Dash => _dash;
    public override bool FastFall => _fastFall;
    public override bool Attack => _attack;
}