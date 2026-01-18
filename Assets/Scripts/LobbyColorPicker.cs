using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class LobbyColorPicker : MonoBehaviour
{
    [SerializeField] private FlexibleColorPicker colorPicker;

    private Image _image;
    private bool _isColorPickerOpen = false;

    private void Awake()
    {
        _image = GetComponent<Image>();
        colorPicker.onColorChange.AddListener(OnColorChanged);
    }

    private void OnColorChanged(Color color)
    {
        _image.color = color;
    }

    private void OnDestroy()
    {
        colorPicker.onColorChange.RemoveListener(OnColorChanged);
    }

    private void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            
            if (_isColorPickerOpen)
            {
                if (!RectTransformUtility.RectangleContainsScreenPoint(colorPicker.GetComponent<RectTransform>(), mousePosition))
                {
                    CloseColorPicker();
                }
            }
            else
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(GetComponent<RectTransform>(), mousePosition))
                {
                    OpenColorPicker();
                }
            }
        }
    }

    private void OpenColorPicker()
    {
        colorPicker.gameObject.SetActive(true);
        _isColorPickerOpen = true;
    }

    private void CloseColorPicker()
    {
        colorPicker.gameObject.SetActive(false);
        _isColorPickerOpen = false;
    }
}
