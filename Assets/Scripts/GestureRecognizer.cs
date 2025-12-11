using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public static class GestureRecognizer
{
    private const int NumPoints = 64;
    private static readonly List<List<Vector2>> templates = new();

    static GestureRecognizer()
    {
        templates.Add(Normalize(GeneratePerfectCircle()));
        templates.Add(Normalize(GeneratePerfectSquare()));
        templates.Add(Normalize(GeneratePerfectTriangle()));
    }

    public static (string name, float score) Recognize(List<Vector2> points)
    {
        if (points == null || points.Count < 20)
            return ("Рисуй больше!", 0f);

        var candidate = Normalize(points);

        float bestDistance = float.MaxValue;
        int bestIndex = 0;

        for (int i = 0; i < templates.Count; i++)
        {
            float dist = DistanceAtBestAngle(candidate, templates[i]);
            if (dist < bestDistance)
            {
                bestDistance = dist;
                bestIndex = i;
            }
        }

        float score = 1f - (bestDistance / (0.5f * Mathf.Sqrt(2f))); // 0..1
        score = Mathf.Clamp01(score);

        string[] names = { "Круг", "Квадрат", "Треугольник" };
        return (names[bestIndex], score);
    }

    // ← КЛЮЧЕВОЕ УЛУЧШЕНИЕ: проверяем угол ±45° (оригинальный $1 алгоритм!)
    private static float DistanceAtBestAngle(List<Vector2> pts1, List<Vector2> pts2)
    {
        float a = -45f * Mathf.Deg2Rad;
        float b =  45f * Mathf.Deg2Rad;
        float threshold = 2f * Mathf.Deg2Rad;

        float best = float.MaxValue;
        for (float angle = a; angle <= b; angle += threshold)
        {
            float dist = PathDistance(pts1, RotateBy(pts2, angle));
            if (dist < best) best = dist;
        }
        return best;
    }

    private static float PathDistance(List<Vector2> a, List<Vector2> b)
    {
        float d = 0f;
        for (int i = 0; i < NumPoints; i++)
            d += Vector2.Distance(a[i], b[i]);
        return d / NumPoints;
    }

    private static List<Vector2> Normalize(List<Vector2> points)
    {
        var resampled = Resample(points, NumPoints);
        var rotated = RotateBy(resampled, -IndicativeAngle(resampled));
        var translated = TranslateToOrigin(rotated);
        return ScaleToUnit(translated);
    }

		private static float PathLength(List<Vector2> points)
		{
				if (points.Count < 2) return 0f;

				float length = 0f;
				for (int i = 1; i < points.Count; i++)
						length += Vector2.Distance(points[i - 1], points[i]);
				return length;
		}

		private static List<Vector2> Resample(List<Vector2> points, int n)
		{
				float totalLength = PathLength(points);
				
				// Защита от нулевой длины (одна точка или все точки одинаковые)
				if (totalLength <= 0.0001f)
				{
						// Просто дублируем первую точку n раз
						var result = new List<Vector2>();
						for (int i = 0; i < n; i++)
								result.Add(points[0]);
						return result;
				}

				float I = totalLength / (n - 1);  // Теперь безопасно!
				var resampled = new List<Vector2> { points[0] };
				float D = 0f;

				for (int i = 1; i < points.Count; i++)
				{
						Vector2 p1 = points[i - 1];
						Vector2 p2 = points[i];
						float d = Vector2.Distance(p1, p2);

						if (d + D >= I)
						{
								float t = (I - D) / d;
								Vector2 q = Vector2.Lerp(p1, p2, t);
								resampled.Add(q);
								points.Insert(i, q);  // модифицируем исходный список, чтобы цикл шёл дальше
								D = 0f;
						}
						else
						{
								D += d;
						}
				}

				// Если чуть не хватает точек — дублируем последнюю
				while (resampled.Count < n)
						resampled.Add(resampled[resampled.Count - 1]);

				return resampled;
		}

    private static float IndicativeAngle(List<Vector2> points)
    {
        Vector2 c = Centroid(points);
        return Mathf.Atan2(points[0].y - c.y, points[0].x - c.x);
    }

    private static Vector2 Centroid(List<Vector2> points)
    {
        Vector2 sum = Vector2.zero;
        foreach (var p in points) sum += p;
        return sum / points.Count;
    }

    private static List<Vector2> TranslateToOrigin(List<Vector2> points)
    {
        Vector2 c = Centroid(points);
        return points.Select(p => p - c).ToList();
    }

    private static List<Vector2> ScaleToUnit(List<Vector2> points)
    {
        float maxDist = points.Max(p => p.magnitude);
        return points.Select(p => p / maxDist).ToList();
    }

    private static List<Vector2> RotateBy(List<Vector2> points, float angle)
    {
        float c = Mathf.Cos(angle), s = Mathf.Sin(angle);
        return points.Select(p => new Vector2(c * p.x - s * p.y, s * p.x + c * p.y)).ToList();
    }

    // ← ИДЕАЛЬНЫЕ эталоны (по 64 точкам)
    private static List<Vector2> GeneratePerfectCircle()
    {
        var pts = new List<Vector2>();
        for (int i = 0; i < NumPoints; i++)
        {
            float a = 2f * Mathf.PI * i / NumPoints;
            pts.Add(new Vector2(Mathf.Cos(a), Mathf.Sin(a)));
        }
        return pts;
    }

    private static List<Vector2> GeneratePerfectSquare()
    {
        var pts = new List<Vector2>();
        int s = NumPoints / 4;
        for (int i = 0; i < s; i++) pts.Add(new Vector2(-1 + 2f * i / s,  1)); // top
        for (int i = 0; i < s; i++) pts.Add(new Vector2( 1,  1 - 2f * i / s)); // right
        for (int i = 0; i < s; i++) pts.Add(new Vector2( 1 - 2f * i / s, -1)); // bottom
        for (int i = 0; i < s; i++) pts.Add(new Vector2(-1, -1 + 2f * i / s)); // left
        return pts;
    }

    private static List<Vector2> GeneratePerfectTriangle()
    {
        var pts = new List<Vector2>();
        int s = NumPoints / 3;
        // от левого угла к вершине
        for (int i = 0; i <= s; i++) pts.Add(new Vector2(-1 + 2f * i / s, -1));
        // от вершины к правому углу
        for (int i = 1; i <= s; i++) pts.Add(new Vector2(1 - 4f * i / s, 1));
        // от правого угла к левому
        for (int i = 1; i < s; i++) pts.Add(new Vector2(-1 + 4f * i / s, -1));
        return pts;
    }
}