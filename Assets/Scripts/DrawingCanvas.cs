using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

[RequireComponent(typeof(RawImage))]
public class DrawingCanvas : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    private RawImage rawImage;
    private Texture2D texture;
    private List<Vector2> points = new();
    private Vector2 currentPoint;
    private bool isDrawing = false;
    private RectTransform rectTransform;

    public List<Vector2> GetPoints() => new List<Vector2>(points);  // Копия
    public void Clear() 
    { 
        points.Clear(); 
        Draw(); 
    }

    void OnEnable()  // ← КЛЮЧЕВОЙ ФИКС: вызывается при SetActive(true)!
    {
        rectTransform = GetComponent<RectTransform>();
        rawImage = GetComponent<RawImage>();
        if (texture == null)
        {
            texture = new Texture2D(400, 400, TextureFormat.RGBA32, false);
            rawImage.texture = texture;
        }
        Clear();  // Теперь безопасно
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isDrawing = true;
        AddPoint(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isDrawing) AddPoint(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDrawing = false;
        AddPoint(eventData);
    }

    private void AddPoint(PointerEventData eventData)
    {
        Vector2 localPos = rectTransform.InverseTransformPoint(eventData.position);
        // ← ФИКС: Нормализуем в [-0.5 .. 0.5]
        currentPoint = new Vector2(
            localPos.x / rectTransform.sizeDelta.x,
            localPos.y / rectTransform.sizeDelta.y
        );
        points.Add(currentPoint);
        Draw();
    }

    private void Draw()
    {
        Color[] pixels = new Color[texture.width * texture.height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.black;

        if (points.Count > 1)
        {
            for (int i = 1; i < points.Count; i++)
            {
                DrawLine(pixels, WorldToPixel(points[i - 1]), WorldToPixel(points[i]), Color.white);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
    }

    private Vector2Int WorldToPixel(Vector2 world)
    {
        // ← ФИКС: Правильные [0..1] UV → пиксели с clamp
        float px = (world.x + 0.5f) * texture.width;
        float py = (world.y + 0.5f) * texture.height;
        int x = Mathf.Clamp(Mathf.RoundToInt(px), 0, texture.width - 1);
        int y = Mathf.Clamp(Mathf.RoundToInt(py), 0, texture.height - 1);
        return new Vector2Int(x, y);
    }

    private void DrawLine(Color[] pixels, Vector2Int p0, Vector2Int p1, Color color)
    {
        int x0 = p0.x, y0 = p0.y, x1 = p1.x, y1 = p1.y;
        int dx = Mathf.Abs(x1 - x0), dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            if (x0 >= 0 && x0 < texture.width && y0 >= 0 && y0 < texture.height)
                pixels[y0 * texture.width + x0] = color;

            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 < dx) { err += dx; y0 += sy; }
        }
    }
}