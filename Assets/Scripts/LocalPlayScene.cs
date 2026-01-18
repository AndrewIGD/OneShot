using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LocalPlayScene : MonoBehaviour
{
    [SerializeField] private LocalFightManager localFightManager;
    [SerializeField] private InputDeviceManager inputDeviceManager;
    [SerializeField] private Transform playersContainer;
    [SerializeField] private LobbyPlayer playerPrefab;

    private List<LobbyPlayer> players;

    private bool _isLoading = false;
    
    private void Start()
    {
        players = new List<LobbyPlayer>();

        inputDeviceManager.OnDeviceAdded += OnDeviceAdded;
        inputDeviceManager.OnDeviceRemoved += OnDeviceRemoved;

        foreach (var device in inputDeviceManager.inputDevices)
        {
            OnDeviceAdded(device);
        }
    }

    private void OnDestroy()
    {
        inputDeviceManager.OnDeviceAdded -= OnDeviceAdded;
        inputDeviceManager.OnDeviceRemoved -= OnDeviceRemoved;
    }

    private void OnDeviceAdded(InputDevice device)
    {
        var player = Instantiate(playerPrefab, playersContainer);
        player.Setup(device);
        players.Add(player);
    }
    
    private void OnDeviceRemoved(InputDevice device)
    {
        var player = players.Find(p => p.Device == device);
        if (player != null)
        {
            Destroy(player.gameObject);
            players.Remove(player);
        }
    }

    public async void Play()
    {
        if (_isLoading)
            return;

        _isLoading = true;

        var playerAppearances = new List<PlayerAppearance>();
        foreach (var player in players)
        {
            if (player == null || player.Device == null)
                continue;

            var playerAppearance = new PlayerAppearance();
            playerAppearance.name = player.Name;
            playerAppearance.color = player.Color;
            playerAppearance.inputDevice = player.Device;

            playerAppearances.Add(playerAppearance);
        }

        localFightManager.SetPlayers(playerAppearances);
        await SceneManager.LoadSceneAsync("FightScene");

        await Task.Yield();

        await localFightManager.OnFightSceneLoaded();

        _isLoading = false;
    }
}
