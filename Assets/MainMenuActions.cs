using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu(fileName = "MainMenuActions", menuName = "MainMenuActions")]
public class MainMenuActions : ScriptableObject
{
    public void PlayLocally()
    {
        SceneManager.LoadScene("LocalPlay");
    }
}
