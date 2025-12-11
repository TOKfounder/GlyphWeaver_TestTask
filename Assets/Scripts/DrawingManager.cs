using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DrawingManager : MonoBehaviour
{
    [SerializeField] private GameObject drawingPanel;
    [SerializeField] private Button drawButton;
    [SerializeField] private Button doneButton;
    [SerializeField] private Button clearButton;   // ← НОВАЯ: Очистить
    [SerializeField] private Button closeButton;   // ← НОВАЯ: Закрыть
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private DrawingCanvas drawingCanvas;

    void Start()
    {
        drawButton.onClick.AddListener(ShowDrawingPanel);
        doneButton.onClick.AddListener(Recognize);  // ← Без Hide!
        clearButton.onClick.AddListener(ClearCanvas);
        closeButton.onClick.AddListener(HideDrawingPanel);
    }

    void ShowDrawingPanel() 
    { 
        drawingPanel.SetActive(true); 
        drawingCanvas.Clear(); 
        resultText.text = "";  // Сброс результата при открытии
    }
		void Recognize()
		{
				var points = drawingCanvas.GetPoints();
				var (name, score) = GestureRecognizer.Recognize(points);

				// ← Если мало точек — защита
				if (points.Count < 20)
						resultText.text = "Слишком коротко! Рисуй больше";
				else
						resultText.text = $"{name}: {(score * 100):F1}%";

				drawingCanvas.Clear();           // очищаем холст
				resultText.text = resultText.text; // (необязательно, просто обновляем)
		}

		void ClearCanvas()
		{
				drawingCanvas.Clear();
				resultText.text = "";            // ← КЛЮЧЕВОЙ ФИКС: сбрасываем старый результат!
		}
    void HideDrawingPanel()
    {
        drawingPanel.SetActive(false);
    }
}