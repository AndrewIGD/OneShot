using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "InputDeviceManager", menuName = "InputDeviceManager")]
public class InputDeviceManager : IUpdateable
{
    public List<InputDevice> inputDevices;

    public event Action<InputDevice> OnDeviceAdded;
    public event Action<InputDevice> OnDeviceRemoved;

    private void OnEnable()
    {
        inputDevices = new List<InputDevice>();

        foreach (var device in InputSystem.devices)
        {
            if (device is Gamepad)
            {
                AddGamepad(device as Gamepad);
            }
            else if (device is Keyboard)
            {
                AddKeyboard(device as Keyboard);
            }
            else if (device is Joystick)
            {
                AddJoystick(device as Joystick);
            }
        }

        InputSystem.onDeviceChange += (device, change) =>
        {
            if (device is Gamepad)
            {
                switch (change)
                {
                    case InputDeviceChange.Added:
                        AddGamepad(device as Gamepad);
                        break;

                    case InputDeviceChange.Removed:
                        RemoveGamepad(device as Gamepad);
                        break;
                }
            }
            else if (device is Keyboard)
            {
                switch (change)
                {
                    case InputDeviceChange.Added:
                        AddKeyboard(device as Keyboard);
                        break;

                    case InputDeviceChange.Removed:
                        RemoveKeyboard(device as Keyboard);
                        break;
                }
            }
            else if (device is Joystick)
            {
                switch (change)
                {
                    case InputDeviceChange.Added:
                        AddJoystick(device as Joystick);
                        break;

                    case InputDeviceChange.Removed:
                        RemoveJoystick(device as Joystick);
                        break;
                }
            }
        };
    }

    private void OnDisable()
    {
        inputDevices.Clear();
    }

    private void AddGamepad(Gamepad gamepad)
    {
        if (inputDevices.Find(d => d is GamepadInputDevice gamepadInputDevice && gamepadInputDevice.Gamepad.deviceId == gamepad.deviceId) != null)
        {
            return;
        }

        inputDevices.Add(new GamepadInputDevice(gamepad));
        OnDeviceAdded?.Invoke(inputDevices.Last());
    }

    private void RemoveGamepad(Gamepad gamepad)
    {
        var device = inputDevices.Find(d => d is GamepadInputDevice gamepadInputDevice && gamepadInputDevice.Gamepad.deviceId == gamepad.deviceId);
        if (device != null)
        {
            inputDevices.Remove(device);
            OnDeviceRemoved?.Invoke(device);
        }
    }

    private void AddKeyboard(Keyboard keyboard)
    {
        if (inputDevices.Find(d => d is KeyboardInputDevice keyboardInputDevice && keyboardInputDevice.Keyboard.deviceId == keyboard.deviceId) != null)
        {
            return;
        }

        inputDevices.Add(new KeyboardInputDevice(keyboard));
        OnDeviceAdded?.Invoke(inputDevices.Last());
    }

    private void RemoveKeyboard(Keyboard keyboard)
    {
        var device = inputDevices.Find(d => d is KeyboardInputDevice keyboardInputDevice && keyboardInputDevice.Keyboard == keyboard);
        if (device != null)
        {
            inputDevices.Remove(device);
            OnDeviceRemoved?.Invoke(device);
        }
    }

    private void AddJoystick(Joystick joystick)
    {
        if (inputDevices.Find(d => d is JoystickInputDevice joystickInputDevice && joystickInputDevice.Joystick.deviceId == joystick.deviceId) != null)
        {
            return;
        }

        inputDevices.Add(new JoystickInputDevice(joystick));
        OnDeviceAdded?.Invoke(inputDevices.Last());
    }

    private void RemoveJoystick(Joystick joystick)
    {
        var device = inputDevices.Find(d => d is JoystickInputDevice joystickInputDevice && joystickInputDevice.Joystick.deviceId == joystick.deviceId);
        if (device != null)
        {
            inputDevices.Remove(device);
            OnDeviceRemoved?.Invoke(device);
        }
    }

    public override void Update()
    {
        foreach (var device in inputDevices)
        {
            device.Update();
        }
    }
}
