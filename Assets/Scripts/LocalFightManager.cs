using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "LocalFightManager", menuName = "LocalFightManager")]
public class LocalFightManager : ScriptableObject
{
    [SerializeField] private Player _playerPrefab;

    private List<PlayerAppearance> _players = new List<PlayerAppearance>();

    public void SetPlayers(List<PlayerAppearance> players)
    {
        _players = players;
    }

    public async Task OnFightSceneLoaded()
    {
        await Task.Yield();

        foreach (var player in _players)
        {
            var playerInstance = Instantiate(_playerPrefab);
            playerInstance.transform.position = GameManager.Instance.GetRandomSpawnPosition();
            playerInstance.ChangeAppearance(player.name, player.color);
            playerInstance.SetInputDevice(player.inputDevice);
        }
    }
}
