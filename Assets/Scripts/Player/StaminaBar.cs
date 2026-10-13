using UnityEngine;

public class StaminaBar : MonoBehaviour
{
    private const float BarWidth = 1.2f;
    private const float BarHeight = 0.12f;
    private const float BorderPadding = 0.03f;
    private const float TickWidth = 0.02f;

    private Player _player;
    private Vector3 _offset;
    private Transform _fillTransform;
    private SpriteRenderer _fillRenderer;

    public void Initialize(Player player, Vector3 offset)
    {
        _player = player;
        _offset = offset;

        var backgroundSprite = CreateSquareSprite(new Vector2(0.5f, 0.5f));
        var fillSprite = CreateSquareSprite(new Vector2(0f, 0.5f));

        var borderObject = new GameObject("Border");
        borderObject.transform.SetParent(transform);
        borderObject.transform.localPosition = new Vector3(0f, 0f, 0.02f);
        borderObject.transform.localScale = new Vector3(BarWidth + BorderPadding * 2f, BarHeight + BorderPadding * 2f, 1f);
        var borderRenderer = borderObject.AddComponent<SpriteRenderer>();
        borderRenderer.sprite = backgroundSprite;
        borderRenderer.color = new Color(0.08f, 0.08f, 0.08f, 0.9f);
        borderRenderer.sortingOrder = 10;

        var backgroundObject = new GameObject("Background");
        backgroundObject.transform.SetParent(transform);
        backgroundObject.transform.localPosition = new Vector3(0f, 0f, 0.01f);
        backgroundObject.transform.localScale = new Vector3(BarWidth, BarHeight, 1f);
        var backgroundRenderer = backgroundObject.AddComponent<SpriteRenderer>();
        backgroundRenderer.sprite = backgroundSprite;
        backgroundRenderer.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        backgroundRenderer.sortingOrder = 11;

        var fillObject = new GameObject("Fill");
        fillObject.transform.SetParent(transform);
        fillObject.transform.localPosition = new Vector3(-BarWidth / 2f, 0f, 0f);
        fillObject.transform.localScale = new Vector3(BarWidth, BarHeight, 1f);
        _fillTransform = fillObject.transform;
        _fillRenderer = fillObject.AddComponent<SpriteRenderer>();
        _fillRenderer.sprite = fillSprite;
        _fillRenderer.color = new Color(0.18f, 0.8f, 0.44f, 1f);
        _fillRenderer.sortingOrder = 12;

        for (var i = 1; i < 5; i++)
        {
            var tickObject = new GameObject($"Tick_{i}");
            tickObject.transform.SetParent(transform);
            var tickX = -BarWidth / 2f + BarWidth * (i * 0.2f);
            tickObject.transform.localPosition = new Vector3(tickX, 0f, -0.01f);
            tickObject.transform.localScale = new Vector3(TickWidth, BarHeight, 1f);
            var tickRenderer = tickObject.AddComponent<SpriteRenderer>();
            tickRenderer.sprite = backgroundSprite;
            tickRenderer.color = new Color(0.08f, 0.08f, 0.08f, 0.95f);
            tickRenderer.sortingOrder = 13;
        }
    }

    private void LateUpdate()
    {
        transform.position = _player.transform.position + _offset;
        transform.rotation = Quaternion.identity;

        var stamina = _player.Stamina;
        _fillTransform.localScale = new Vector3(BarWidth * stamina, BarHeight, 1f);
        _fillRenderer.color = GetStaminaColor(stamina);
    }

    private Color GetStaminaColor(float stamina)
    {
        if (stamina > 0.6f)
        {
            var t = (stamina - 0.6f) / 0.4f;
            return Color.Lerp(new Color(0.95f, 0.77f, 0.06f), new Color(0.18f, 0.8f, 0.44f), t);
        }

        var lowerT = (stamina - 0.2f) / 0.4f;
        return Color.Lerp(new Color(0.91f, 0.3f, 0.24f), new Color(0.95f, 0.77f, 0.06f), lowerT);
    }

    private Sprite CreateSquareSprite(Vector2 pivot)
    {
        var texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 1, 1), pivot, 1f);
    }
}
