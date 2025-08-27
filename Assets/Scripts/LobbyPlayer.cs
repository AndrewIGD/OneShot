using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPlayer : MonoBehaviour
{
    [SerializeField] private Image inputImage;
    [SerializeField] private Image colorImage;
    [SerializeField] private TMP_InputField nameInput;

    public string Name => nameInput.text;
    public Color Color => colorImage.color;

    public InputDevice Device { get; private set; }

    public void Setup(InputDevice device)
    {
        Device = device;
    }

    private void Update()
    {
        inputImage.color = Device.Any ? Color.green : Color.red;
    }
}
