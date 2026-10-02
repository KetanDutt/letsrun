using UnityEngine;
using UnityEngine.UI;

/// <summary>A tiny reused uGUI confetti pool, rendered above the results card.</summary>
public sealed class UIConfetti : MonoBehaviour
{
    private sealed class Piece
    {
        public RectTransform rect;
        public Image image;
        public Vector2 velocity;
        public Color color;
        public float life, spin;
    }
    private readonly Piece[] pieces = new Piece[20];
    private RectTransform area;

    public void Initialize(RectTransform parent)
    {
        area = UIFactory.Rect(parent, "Celebration Layer", 0f, 0f, 1f, 1f);
        area.SetAsFirstSibling();
        for (int i = 0; i < pieces.Length; i++)
        {
            Image image = UIFactory.Image(area, "Confetti " + i, UIFactory.Gold, 0.5f, 0.5f, 0.5f, 0.5f, false);
            image.rectTransform.sizeDelta = new Vector2(10f, 17f);
            image.gameObject.SetActive(false);
            pieces[i] = new Piece { image = image, rect = image.rectTransform };
        }
        enabled = false;
    }

    public void Burst()
    {
        if (area == null || (GameManager.instance != null && GameManager.instance.ReducedMotion)) return;
        for (int i = 0; i < pieces.Length; i++)
        {
            Piece piece = pieces[i];
            piece.life = 1.1f;
            piece.color = i % 3 == 0 ? UIFactory.Coral : i % 3 == 1 ? UIFactory.Teal : UIFactory.Gold;
            piece.image.color = piece.color;
            piece.velocity = new Vector2(Random.Range(-160f, 160f), Random.Range(140f, 320f));
            piece.spin = Random.Range(-240f, 240f);
            piece.rect.anchoredPosition = new Vector2(Random.Range(-0.3f, 0.3f) * area.rect.width, area.rect.height * 0.19f);
            piece.image.gameObject.SetActive(true);
        }
        enabled = true;
    }

    private void Update()
    {
        float delta = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
        bool any = false;
        bool reduced = GameManager.instance != null && GameManager.instance.ReducedMotion;
        for (int i = 0; i < pieces.Length; i++)
        {
            Piece piece = pieces[i];
            if (piece.life <= 0f) continue;
            piece.life = reduced ? 0f : piece.life - delta;
            if (piece.life <= 0f) { piece.image.gameObject.SetActive(false); continue; }
            any = true;
            piece.velocity += Vector2.down * (600f * delta);
            piece.rect.anchoredPosition += piece.velocity * delta;
            piece.rect.Rotate(0f, 0f, piece.spin * delta);
            Color color = piece.color;
            color.a = Mathf.Clamp01(piece.life / 0.4f);
            piece.image.color = color;
        }
        if (!any) enabled = false;
    }
}
