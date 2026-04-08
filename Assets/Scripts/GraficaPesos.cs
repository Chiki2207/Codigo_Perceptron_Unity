using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace PerceptronSimulator
{
    /// <summary>
    /// Dibuja gráfica de pesos como UI 2D (RawImage + Texture2D):
    /// 4 líneas (W1 azul, W2 verde, W3 amarillo, Bias blanco), fondo oscuro.
    /// Para 3 clases: selector para ver perceptrón 1, 2 o 3.
    /// </summary>
    public class GraficaPesos : MonoBehaviour
    {
        [Header("UI")]
        public RawImage rawImage;

        [Header("Textura")]
        public int textureWidth = 512;
        public int textureHeight = 256;
        [Range(0, 64)] public int padding = 12;
        public Color backgroundColor = new Color(0.08f, 0.08f, 0.08f, 1f);
        public Color axesColor = new Color(0.35f, 0.35f, 0.35f, 1f);
        public Color gridColor = new Color(0.22f, 0.22f, 0.22f, 1f);
        public Color labelColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        public bool drawAxes = true;
        public bool drawGrid = true;
        [Range(2, 10)] public int gridLines = 5;

        [Header("Colores líneas")]
        public Color colorW1 = Color.blue;
        public Color colorW2 = Color.green;
        public Color colorW3 = Color.yellow;
        public Color colorBias = Color.white;

        [Header("3 clases - selector")]
        public Dropdown dropdownPerceptron;
        public Text labelPerceptron;

        private List<PesoEpoca> _p1, _p2, _p3;
        private int _perceptronActual = 0;

        private Texture2D _tex;
        private Color32[] _pixels;

        private const int FontW = 5;
        private const int FontH = 7;
        private const int CharSpacing = 1;

        public void Clear()
        {
            EnsureTexture();
            Fill(backgroundColor);
            if (drawAxes) DrawAxesAndDecorations(0f, 1f, 0, 1);
            Apply();
            _p1 = _p2 = _p3 = null;
            if (dropdownPerceptron != null) dropdownPerceptron.gameObject.SetActive(false);
        }

        public void Dibujar(List<PesoEpoca> pesosPorEpoca)
        {
            EnsureTexture();
            Fill(backgroundColor);
            if (pesosPorEpoca == null || pesosPorEpoca.Count == 0) return;
            ComputeMinMax(pesosPorEpoca, out float minW, out float maxW);
            if (drawAxes) DrawAxesAndDecorations(minW, maxW, 1, pesosPorEpoca.Count);
            DibujarPesosInterno(pesosPorEpoca);
            Apply();
        }

        /// <summary>Para 3 clases: guarda los 3 historiales y muestra el seleccionado.</summary>
        public void Dibujar3Clases(List<PesoEpoca> p1, List<PesoEpoca> p2, List<PesoEpoca> p3)
        {
            _p1 = p1;
            _p2 = p2;
            _p3 = p3;
            if (dropdownPerceptron != null)
            {
                dropdownPerceptron.gameObject.SetActive(true);
                dropdownPerceptron.ClearOptions();
                dropdownPerceptron.AddOptions(new List<string> { "Perceptrón 1", "Perceptrón 2", "Perceptrón 3" });
                dropdownPerceptron.onValueChanged.RemoveAllListeners();
                dropdownPerceptron.onValueChanged.AddListener(OnPerceptronSelected);
                _perceptronActual = dropdownPerceptron.value;
            }
            var list = _perceptronActual == 0 ? _p1 : (_perceptronActual == 1 ? _p2 : _p3);
            EnsureTexture();
            Fill(backgroundColor);
            if (list != null) DibujarPesosInterno(list);
            if (list != null && list.Count > 0)
            {
                ComputeMinMax(list, out float minW, out float maxW);
                if (drawAxes) DrawAxesAndDecorations(minW, maxW, 1, list.Count);
                DibujarPesosInterno(list);
            }
            Apply();
            if (labelPerceptron != null) labelPerceptron.text = "P" + (_perceptronActual + 1);
        }

        private void OnPerceptronSelected(int index)
        {
            _perceptronActual = index;
            var list = _perceptronActual == 0 ? _p1 : (_perceptronActual == 1 ? _p2 : _p3);
            EnsureTexture();
            Fill(backgroundColor);
            if (list != null && list.Count > 0)
            {
                ComputeMinMax(list, out float minW, out float maxW);
                if (drawAxes) DrawAxesAndDecorations(minW, maxW, 1, list.Count);
                DibujarPesosInterno(list);
            }
            Apply();
            if (labelPerceptron != null) labelPerceptron.text = "P" + (_perceptronActual + 1);
        }

        private void DibujarPesosInterno(List<PesoEpoca> pesosYBiasPorEpoca)
        {
            if (pesosYBiasPorEpoca == null || pesosYBiasPorEpoca.Count == 0) return;
            ComputeMinMax(pesosYBiasPorEpoca, out float minW, out float maxW);

            int n = pesosYBiasPorEpoca.Count;
            var s1 = new List<float>(n);
            var s2 = new List<float>(n);
            var s3 = new List<float>(n);
            var sb = new List<float>(n);
            for (int i = 0; i < n; i++)
            {
                var e = pesosYBiasPorEpoca[i];
                float v1 = e?.pesos != null && e.pesos.Count > 0 ? e.pesos[0] : 0;
                float v2 = e?.pesos != null && e.pesos.Count > 1 ? e.pesos[1] : 0;
                float v3 = e?.pesos != null && e.pesos.Count > 2 ? e.pesos[2] : 0;
                float vb = e?.bias ?? 0;
                s1.Add(v1);
                s2.Add(v2);
                s3.Add(v3);
                sb.Add(vb);
            }

            PlotLine(s1, minW, maxW, colorW1);
            PlotLine(s2, minW, maxW, colorW2);
            PlotLine(s3, minW, maxW, colorW3);
            PlotLine(sb, minW, maxW, colorBias);
        }

        private void EnsureTexture()
        {
            if (rawImage == null) return;
            if (_tex != null && _tex.width == textureWidth && _tex.height == textureHeight && _pixels != null && _pixels.Length == textureWidth * textureHeight)
                return;

            _tex = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            _pixels = new Color32[textureWidth * textureHeight];
            rawImage.texture = _tex;
            Clear();
        }

        private void Fill(Color c)
        {
            if (_pixels == null) return;
            var c32 = (Color32)c;
            for (int i = 0; i < _pixels.Length; i++) _pixels[i] = c32;
        }

        private void Apply()
        {
            if (_tex == null || _pixels == null || rawImage == null) return;
            _tex.SetPixels32(_pixels);
            _tex.Apply(false, false);
        }

        private void DrawAxes()
        {
            DrawAxesAndDecorations(0f, 1f, 0, 1);
        }

        private void PlotLine(List<float> values, float minY, float maxY, Color color)
        {
            EnsureTexture();
            if (_tex == null || _pixels == null) return;
            if (values == null || values.Count < 2) return;

            GetPlotRect(out int left, out int right, out int bottom, out int top);
            int width = Mathf.Max(1, right - left);
            int height = Mathf.Max(1, top - bottom);

            float range = Mathf.Max(0.0001f, maxY - minY);
            int n = values.Count;

            int prevX = left;
            int prevY = bottom + Mathf.RoundToInt(Mathf.Clamp01((values[0] - minY) / range) * height);
            for (int i = 1; i < n; i++)
            {
                float t = (float)i / (n - 1);
                int x = left + Mathf.RoundToInt(t * width);
                int y = bottom + Mathf.RoundToInt(Mathf.Clamp01((values[i] - minY) / range) * height);
                DrawLine(prevX, prevY, x, y, color);
                prevX = x;
                prevY = y;
            }
        }

        private void DrawLine(int x0, int y0, int x1, int y1, Color c)
        {
            var c32 = (Color32)c;
            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                SetPixelSafe(x0, y0, c32);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
            }
        }

        private void SetPixelSafe(int x, int y, Color32 c)
        {
            if (_pixels == null) return;
            if (x < 0 || x >= textureWidth || y < 0 || y >= textureHeight) return;
            _pixels[y * textureWidth + x] = c;
        }

        private void ComputeMinMax(List<PesoEpoca> pesosYBiasPorEpoca, out float minW, out float maxW)
        {
            minW = float.MaxValue;
            maxW = float.MinValue;
            foreach (var e in pesosYBiasPorEpoca)
            {
                if (e?.pesos != null)
                {
                    foreach (float v in e.pesos) { minW = Mathf.Min(minW, v); maxW = Mathf.Max(maxW, v); }
                }
                minW = Mathf.Min(minW, e.bias);
                maxW = Mathf.Max(maxW, e.bias);
            }
            if (minW == float.MaxValue) { minW = 0f; maxW = 1f; }
            if (maxW <= minW) maxW = minW + 1f;
        }

        private void GetPlotRect(out int left, out int right, out int bottom, out int top)
        {
            int titleH = FontH + 2;
            int xLabelH = FontH + 3;
            int yLabelW = (FontW + CharSpacing) * 7 + 2; // hasta "-999.9"

            left = Mathf.Clamp(padding + yLabelW, 0, textureWidth - 1);
            right = Mathf.Clamp(textureWidth - 1 - padding, 0, textureWidth - 1);
            bottom = Mathf.Clamp(padding + xLabelH, 0, textureHeight - 1);
            top = Mathf.Clamp(textureHeight - 1 - padding - titleH, 0, textureHeight - 1);

            if (right <= left) right = Mathf.Min(textureWidth - 1, left + 1);
            if (top <= bottom) top = Mathf.Min(textureHeight - 1, bottom + 1);
        }

        private void DrawAxesAndDecorations(float minY, float maxY, int minX, int maxX)
        {
            GetPlotRect(out int left, out int right, out int bottom, out int top);

            DrawTextCentered("Pesos por epoca", (left + right) / 2, top + padding + 2, labelColor);

            // Leyenda: esquina superior derecha (dentro del área del gráfico)
            int legendPadding = 4;
            int boxSize = 6;
            int legendLineH = FontH + 2;
            string[] labels = { "W1", "W2", "W3", "Bias" };
            Color[] colors = { colorW1, colorW2, colorW3, colorBias };
            int maxLabelW = 0;
            for (int i = 0; i < labels.Length; i++) maxLabelW = Mathf.Max(maxLabelW, MeasureTextWidth(labels[i]));
            int legendW = boxSize + 2 + maxLabelW + 2 * legendPadding;
            int legendH = labels.Length * legendLineH + 2 * legendPadding;

            int legendX = right - legendW - 2;
            int legendY = top - legendH - 2;
            FillRect(legendX, legendY, legendW, legendH, new Color(0f, 0f, 0f, 0.35f));
            DrawRect(legendX, legendY, legendW, legendH, axesColor);

            int cx = legendX + legendPadding;
            int cy = legendY + legendPadding;
            for (int i = 0; i < labels.Length; i++)
            {
                FillRect(cx, cy + 1, boxSize, boxSize, colors[i]);
                DrawRect(cx, cy + 1, boxSize, boxSize, labelColor);
                DrawText(labels[i], cx + boxSize + 2, cy, labelColor);
                cy += legendLineH;
            }

            DrawLine(left, bottom, right, bottom, axesColor);
            DrawLine(left, bottom, left, top, axesColor);

            if (maxY <= minY) maxY = minY + 1f;
            float yStep = NiceStep((maxY - minY) / Mathf.Max(1, gridLines - 1));
            float yStart = Mathf.Floor(minY / yStep) * yStep;
            float yEnd = Mathf.Ceil(maxY / yStep) * yStep;

            int lastLabelY = -9999;
            int minLabelGap = FontH + 3;
            for (float yVal = yStart; yVal <= yEnd + 0.0001f; yVal += yStep)
            {
                float t = Mathf.InverseLerp(minY, maxY, yVal);
                int y = bottom + Mathf.RoundToInt(t * (top - bottom));
                if (drawGrid) DrawLine(left, y, right, y, gridColor);
                if (Mathf.Abs(y - lastLabelY) >= minLabelGap)
                {
                    string label = FormatNumber(yVal);
                    DrawTextRight(label, left - 2, y - FontH / 2, labelColor);
                    lastLabelY = y;
                }
            }

            int span = Mathf.Max(1, maxX - minX);
            int desiredTicks = 6;
            int xStep = Mathf.Max(1, Mathf.RoundToInt(NiceStep(span / (float)(desiredTicks - 1))));
            int firstTick = Mathf.CeilToInt(minX / (float)xStep) * xStep;
            int lastDrawnTick = minX;
            for (int xv = firstTick; xv <= maxX; xv += xStep)
            {
                float t = (xv - minX) / (float)span;
                int x = left + Mathf.RoundToInt(t * (right - left));
                DrawLine(x, bottom, x, bottom - 3, axesColor);
                if (drawGrid) DrawLine(x, bottom, x, top, new Color(gridColor.r, gridColor.g, gridColor.b, 0.35f));
                DrawTextCentered(xv.ToString(), x, bottom - (FontH + 4), labelColor);
                lastDrawnTick = xv;
            }
            if (lastDrawnTick < maxX)
            {
                int x = right;
                DrawLine(x, bottom, x, bottom - 3, axesColor);
                DrawTextCentered(maxX.ToString(), x, bottom - (FontH + 4), labelColor);
            }
        }

        private void FillRect(int x, int y, int w, int h, Color c)
        {
            var c32 = (Color32)c;
            int x0 = Mathf.Clamp(x, 0, textureWidth - 1);
            int y0 = Mathf.Clamp(y, 0, textureHeight - 1);
            int x1 = Mathf.Clamp(x + w - 1, 0, textureWidth - 1);
            int y1 = Mathf.Clamp(y + h - 1, 0, textureHeight - 1);
            for (int yy = y0; yy <= y1; yy++)
            {
                int row = yy * textureWidth;
                for (int xx = x0; xx <= x1; xx++)
                    _pixels[row + xx] = c32;
            }
        }

        private void DrawRect(int x, int y, int w, int h, Color c)
        {
            int x0 = x;
            int y0 = y;
            int x1 = x + w - 1;
            int y1 = y + h - 1;
            DrawLine(x0, y0, x1, y0, c);
            DrawLine(x0, y1, x1, y1, c);
            DrawLine(x0, y0, x0, y1, c);
            DrawLine(x1, y0, x1, y1, c);
        }

        private static float NiceStep(float roughStep)
        {
            if (roughStep <= 0) return 1f;
            float exp = Mathf.Floor(Mathf.Log10(roughStep));
            float f = roughStep / Mathf.Pow(10f, exp);
            float niceF = (f < 1.5f) ? 1f : (f < 3f) ? 2f : (f < 7f) ? 5f : 10f;
            return niceF * Mathf.Pow(10f, exp);
        }

        private static string FormatNumber(float v)
        {
            float av = Mathf.Abs(v);
            var inv = CultureInfo.InvariantCulture;
            if (av >= 10f) return Mathf.RoundToInt(v).ToString(inv);
            if (av >= 1f) return (Mathf.Round(v * 10f) / 10f).ToString("0.0", inv);
            return (Mathf.Round(v * 100f) / 100f).ToString("0.00", inv);
        }

        private void DrawTextCentered(string s, int centerX, int y, Color c)
        {
            int w = MeasureTextWidth(s);
            DrawText(s, centerX - w / 2, y, c);
        }

        private void DrawTextRight(string s, int rightX, int y, Color c)
        {
            int w = MeasureTextWidth(s);
            DrawText(s, rightX - w, y, c);
        }

        private int MeasureTextWidth(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0;
            return s.Length * (FontW + CharSpacing) - CharSpacing;
        }

        private void DrawText(string s, int x, int y, Color c)
        {
            if (string.IsNullOrEmpty(s)) return;
            int cursorX = x;
            foreach (char ch in s)
            {
                DrawChar(ch, cursorX, y, c);
                cursorX += FontW + CharSpacing;
            }
        }

        private void DrawChar(char ch, int x, int y, Color c)
        {
            if (!Font.TryGetValue(ch, out byte[] rows))
                rows = Font['?'];

            var c32 = (Color32)c;
            for (int yy = 0; yy < FontH; yy++)
            {
                byte row = rows[yy];
                for (int xx = 0; xx < FontW; xx++)
                {
                    int bit = (row >> (FontW - 1 - xx)) & 1;
                    if (bit == 1) SetPixelSafe(x + xx, y + (FontH - 1 - yy), c32);
                }
            }
        }

        private static readonly Dictionary<char, byte[]> Font = new Dictionary<char, byte[]>
        {
            ['?'] = new byte[] { 0b01110,0b10001,0b00010,0b00100,0b00100,0b00000,0b00100 },
            [' '] = new byte[] { 0,0,0,0,0,0,0 },
            ['-'] = new byte[] { 0,0,0,0b11111,0,0,0 },
            ['.'] = new byte[] { 0,0,0,0,0,0b00110,0b00110 },
            [','] = new byte[] { 0,0,0,0,0,0b00110,0b00100 },
            ['0'] = new byte[] { 0b01110,0b10001,0b10011,0b10101,0b11001,0b10001,0b01110 },
            ['1'] = new byte[] { 0b00100,0b01100,0b00100,0b00100,0b00100,0b00100,0b01110 },
            ['2'] = new byte[] { 0b01110,0b10001,0b00001,0b00010,0b00100,0b01000,0b11111 },
            ['3'] = new byte[] { 0b11110,0b00001,0b00001,0b01110,0b00001,0b00001,0b11110 },
            ['4'] = new byte[] { 0b00010,0b00110,0b01010,0b10010,0b11111,0b00010,0b00010 },
            ['5'] = new byte[] { 0b11111,0b10000,0b11110,0b00001,0b00001,0b10001,0b01110 },
            ['6'] = new byte[] { 0b00110,0b01000,0b10000,0b11110,0b10001,0b10001,0b01110 },
            ['7'] = new byte[] { 0b11111,0b00001,0b00010,0b00100,0b01000,0b01000,0b01000 },
            ['8'] = new byte[] { 0b01110,0b10001,0b10001,0b01110,0b10001,0b10001,0b01110 },
            ['9'] = new byte[] { 0b01110,0b10001,0b10001,0b01111,0b00001,0b00010,0b01100 },
            ['A'] = new byte[] { 0b01110,0b10001,0b10001,0b11111,0b10001,0b10001,0b10001 },
            ['B'] = new byte[] { 0b11110,0b10001,0b10001,0b11110,0b10001,0b10001,0b11110 },
            ['C'] = new byte[] { 0b01110,0b10001,0b10000,0b10000,0b10000,0b10001,0b01110 },
            ['D'] = new byte[] { 0b11100,0b10010,0b10001,0b10001,0b10001,0b10010,0b11100 },
            ['E'] = new byte[] { 0b11111,0b10000,0b10000,0b11110,0b10000,0b10000,0b11111 },
            ['F'] = new byte[] { 0b11111,0b10000,0b10000,0b11110,0b10000,0b10000,0b10000 },
            ['G'] = new byte[] { 0b01110,0b10001,0b10000,0b10111,0b10001,0b10001,0b01110 },
            ['H'] = new byte[] { 0b10001,0b10001,0b10001,0b11111,0b10001,0b10001,0b10001 },
            ['I'] = new byte[] { 0b01110,0b00100,0b00100,0b00100,0b00100,0b00100,0b01110 },
            ['L'] = new byte[] { 0b10000,0b10000,0b10000,0b10000,0b10000,0b10000,0b11111 },
            ['M'] = new byte[] { 0b10001,0b11011,0b10101,0b10101,0b10001,0b10001,0b10001 },
            ['N'] = new byte[] { 0b10001,0b11001,0b10101,0b10011,0b10001,0b10001,0b10001 },
            ['O'] = new byte[] { 0b01110,0b10001,0b10001,0b10001,0b10001,0b10001,0b01110 },
            ['P'] = new byte[] { 0b11110,0b10001,0b10001,0b11110,0b10000,0b10000,0b10000 },
            ['R'] = new byte[] { 0b11110,0b10001,0b10001,0b11110,0b10100,0b10010,0b10001 },
            ['S'] = new byte[] { 0b01110,0b10001,0b10000,0b01110,0b00001,0b10001,0b01110 },
            ['T'] = new byte[] { 0b11111,0b00100,0b00100,0b00100,0b00100,0b00100,0b00100 },
            ['U'] = new byte[] { 0b10001,0b10001,0b10001,0b10001,0b10001,0b10001,0b01110 },
            ['V'] = new byte[] { 0b10001,0b10001,0b10001,0b10001,0b01010,0b01010,0b00100 },
            ['W'] = new byte[] { 0b10001,0b10001,0b10001,0b10101,0b10101,0b11011,0b10001 },
            ['X'] = new byte[] { 0b10001,0b01010,0b00100,0b00100,0b00100,0b01010,0b10001 },
            ['a'] = new byte[] { 0,0,0b01110,0b00001,0b01111,0b10001,0b01111 },
            ['b'] = new byte[] { 0b10000,0b10000,0b11110,0b10001,0b10001,0b10001,0b11110 },
            ['c'] = new byte[] { 0,0,0b01110,0b10001,0b10000,0b10001,0b01110 },
            ['d'] = new byte[] { 0b00001,0b00001,0b01111,0b10001,0b10001,0b10001,0b01111 },
            ['e'] = new byte[] { 0,0,0b01110,0b10001,0b11111,0b10000,0b01110 },
            ['f'] = new byte[] { 0b00110,0b01001,0b01000,0b11100,0b01000,0b01000,0b01000 },
            ['g'] = new byte[] { 0,0,0b01111,0b10001,0b01111,0b00001,0b01110 },
            ['h'] = new byte[] { 0b10000,0b10000,0b10110,0b11001,0b10001,0b10001,0b10001 },
            ['i'] = new byte[] { 0b00100,0,0b01100,0b00100,0b00100,0b00100,0b01110 },
            ['l'] = new byte[] { 0b01100,0b00100,0b00100,0b00100,0b00100,0b00100,0b01110 },
            ['m'] = new byte[] { 0,0,0b11010,0b10101,0b10101,0b10001,0b10001 },
            ['n'] = new byte[] { 0,0,0b10110,0b11001,0b10001,0b10001,0b10001 },
            ['o'] = new byte[] { 0,0,0b01110,0b10001,0b10001,0b10001,0b01110 },
            ['p'] = new byte[] { 0,0,0b11110,0b10001,0b11110,0b10000,0b10000 },
            ['r'] = new byte[] { 0,0,0b10110,0b11001,0b10000,0b10000,0b10000 },
            ['s'] = new byte[] { 0,0,0b01111,0b10000,0b01110,0b00001,0b11110 },
            ['t'] = new byte[] { 0b01000,0b01000,0b11100,0b01000,0b01000,0b01001,0b00110 },
            ['u'] = new byte[] { 0,0,0b10001,0b10001,0b10001,0b10011,0b01101 },
            ['v'] = new byte[] { 0,0,0b10001,0b10001,0b01010,0b01010,0b00100 },
            ['x'] = new byte[] { 0,0,0b10001,0b01010,0b00100,0b01010,0b10001 },
        };
    }
}
