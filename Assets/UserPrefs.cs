using Mirror;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UserPrefs : MonoBehaviour
{
    private TMP_InputField input;
    private TMP_InputField ipInput;
    private FlexibleColorPicker colorPicker;
    private Button serverButton;
    private Button clientButton;

    public static string userName;
    public static Color color = Color.white;

    NetworkManager manager;

    void Awake()
    {
        manager = GetComponent<NetworkManager>();

        GetComponents();
    }

    void GetComponents()
    {
        input = GameObject.Find("NameInput").GetComponent<TMP_InputField>();
        ipInput = GameObject.Find("IPInput").GetComponent<TMP_InputField>();
        colorPicker = GameObject.Find("FlexibleColorPicker").GetComponent<FlexibleColorPicker>();

        serverButton = GameObject.Find("ServerButton").GetComponent<Button>();
        serverButton.onClick.AddListener(Host);

        clientButton = GameObject.Find("ClientButton").GetComponent<Button>();
        clientButton.onClick.AddListener(Join);

        input.text = userName;
        colorPicker.SetColor(color);
    }

    private void OnLevelWasLoaded(int level)
    {
        if (level == 0)
            GetComponents();
    }

    public void Host()
    {
        userName = input.text;
        color = colorPicker.GetColorFullAlpha();

        manager.StartHost();
    }

    public void Join()
    {
        userName = input.text;
        color = colorPicker.GetColorFullAlpha();

        manager.networkAddress = ipInput.text;

        manager.StartClient();
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            if (NetworkServer.active && NetworkClient.isConnected)
            {
                manager.StopHost();
            }
            else if (NetworkClient.isConnected)
            {
                manager.StopClient();
            }
        }
    }
}
