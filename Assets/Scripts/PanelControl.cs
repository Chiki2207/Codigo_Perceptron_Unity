using UnityEngine;
using UnityEngine.UI;
using System.Globalization;

namespace PerceptronSimulator
{
    /// <summary>
    /// Panel izquierdo: parámetros (medias, desviaciones, instancias, épocas), botones Generar y Entrenar.
    /// Panel derecho: evaluación X1, X2, X3 y botón Evaluar (solo después de entrenar).
    /// Toggle 2 clases / 3 clases. Mensajes de estado.
    /// </summary>
    public class PanelControl : MonoBehaviour
    {
        [Header("Bridge y datos")]
        public PythonBridge pythonBridge;
        public PuntosRenderer puntosRenderer;
        public PlanoRenderer planoRenderer;
        public GraficaError graficaError;
        public GraficaPesos graficaPesos;
        [Header("Paneles UI (opcional)")]
        public GameObject panelGraficaError;
        public GameObject panelGraficaPesos;

        [Header("Panel izquierdo - Dos clases")]
        public InputField inputMedia1;
        public InputField inputDesv1;
        public InputField inputMedia2;
        public InputField inputDesv2;
        public InputField inputInstancias;
        public InputField inputEpocas;
        public InputField inputTasa;
        public Button btnGenerar;
        public Button btnEntrenar;

        [Header("Panel izquierdo - Tres clases (adicional)")]
        public InputField inputMedia3;
        public InputField inputDesv3;
        public Toggle toggle3Clases;

        [Header("Panel derecho - Evaluación")]
        public InputField inputX1;
        public InputField inputX2;
        public InputField inputX3;
        public Button btnEvaluar;
        public Text textResultadoEvaluacion;

        [Header("Estado")]
        public Text textStatus;

        private SimuladorData _data;
        private bool _entrenado;
        private string _jsonPath;

        private void Awake()
        {
            _jsonPath = PythonBridge.RutaJson();
            if (btnGenerar != null) btnGenerar.onClick.AddListener(OnGenerar);
            if (btnEntrenar != null) btnEntrenar.onClick.AddListener(OnEntrenar);
            if (btnEvaluar != null) btnEvaluar.onClick.AddListener(OnEvaluar);
            if (puntosRenderer != null && planoRenderer != null)
            {
                planoRenderer.dataScale = puntosRenderer.dataScale;
                planoRenderer.planeContainer = puntosRenderer.pointsContainer != null ? puntosRenderer.pointsContainer : puntosRenderer.transform;
            }
            if (panelGraficaError != null) panelGraficaError.SetActive(false);
            if (panelGraficaPesos != null) panelGraficaPesos.SetActive(false);
            SetStatus("Listo. Configure parámetros y pulse Generar datos.");
        }

        private void SetStatus(string msg)
        {
            if (textStatus != null) textStatus.text = msg;
        }

        private static float ParseFloat(InputField f, float def)
        {
            if (f == null || string.IsNullOrEmpty(f.text)) return def;
            return float.TryParse(f.text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out float v) ? v : def;
        }

        private static int ParseInt(InputField f, int def)
        {
            if (f == null || string.IsNullOrEmpty(f.text)) return def;
            return int.TryParse(f.text, out int v) ? v : def;
        }

        private bool Es3Clases()
        {
            return toggle3Clases != null && toggle3Clases.isOn;
        }

        private void OnGenerar()
        {
            if (pythonBridge == null) { SetStatus("Error: no hay PythonBridge."); return; }
            _entrenado = false;
            if (puntosRenderer != null) puntosRenderer.Clear();
            if (planoRenderer != null) planoRenderer.Clear();
            if (graficaError != null) graficaError.Clear();
            if (graficaPesos != null) graficaPesos.Clear();
            if (panelGraficaError != null) panelGraficaError.SetActive(false);
            if (panelGraficaPesos != null) panelGraficaPesos.SetActive(false);

            float m1 = ParseFloat(inputMedia1, 2f);
            float d1 = ParseFloat(inputDesv1, 0.5f);
            float m2 = ParseFloat(inputMedia2, 8f);
            float d2 = ParseFloat(inputDesv2, 0.5f);
            int minInst = Es3Clases() ? 6 : 4;
            int inst = Mathf.Clamp(ParseInt(inputInstancias, 100), minInst, 500);
            SetStatus("Generando datos...");

            bool ok;
            if (Es3Clases())
            {
                float m3 = ParseFloat(inputMedia3, 5f);
                float d3 = ParseFloat(inputDesv3, 0.5f);
                ok = pythonBridge.Generar3Clases(m1, d1, m2, d2, m3, d3, inst);
            }
            else
            {
                ok = pythonBridge.Generar2Clases(m1, d1, m2, d2, inst);
            }

            if (!ok) { SetStatus("Error al generar (revisa Python y perceptron_engine.py)."); return; }
            CargarYMostrar(soloPuntos: true);
            SetStatus("Datos generados. Pulse Entrenar.");
        }

        private void OnEntrenar()
        {
            if (pythonBridge == null) { SetStatus("Error: no hay PythonBridge."); return; }
            if (!System.IO.File.Exists(_jsonPath)) { SetStatus("Genere datos primero (Generar datos)."); return; }
            string modo = Es3Clases() ? "3clases" : "2clases";
            int epocas = Mathf.Clamp(ParseInt(inputEpocas, 50), 1, 1000);
            float tasa = Mathf.Clamp(ParseFloat(inputTasa, 0.1f), 0.001f, 2f);
            SetStatus("Entrenando...");

            bool ok = pythonBridge.Entrenar(modo, epocas, tasa);
            if (!ok) { SetStatus("Error al entrenar."); return; }
            if (panelGraficaError != null) panelGraficaError.SetActive(true);
            if (panelGraficaPesos != null) panelGraficaPesos.SetActive(true);
            CargarYMostrar(soloPuntos: false);
            _entrenado = true;
            bool convergio = DataConvergio(_data);
            int epocasEjec = EpocasEjecutadas(_data);
            if (convergio)
                SetStatus("Convergió en " + epocasEjec + " épocas ✅");
            else
                SetStatus("No convergió después de " + epocasEjec + " épocas ❌");
        }

        private void CargarYMostrar(bool soloPuntos)
        {
            if (!JsonLoader.Cargar(_jsonPath, out _data, out string err))
            {
                SetStatus("Error cargando: " + err);
                return;
            }
            if (puntosRenderer != null)
            {
                puntosRenderer.Clear();
                if (_data.puntos != null) puntosRenderer.DibujarPuntos(_data.puntos);
                puntosRenderer.DibujarEjes();
            }
            if (soloPuntos)
            {
                if (planoRenderer != null) planoRenderer.Clear();
                if (graficaError != null) graficaError.Clear();
                if (graficaPesos != null) graficaPesos.Clear();
                return;
            }
            if (planoRenderer != null) planoRenderer.DibujarDesdeSimulador(_data);
            if (graficaError != null)
            {
                if (_data.modo == "3clases" && _data.perceptron1 != null)
                    graficaError.Dibujar3Clases(
                        _data.perceptron1.errores, _data.perceptron2?.errores, _data.perceptron3?.errores,
                        _data.perceptron1.convergio, _data.perceptron2?.convergio ?? false, _data.perceptron3?.convergio ?? false,
                        _data.perceptron1.epocasEjecutadas, _data.perceptron2?.epocasEjecutadas ?? 0, _data.perceptron3?.epocasEjecutadas ?? 0);
                else if (_data.errores != null)
                    graficaError.Dibujar(_data.errores, _data.convergio, _data.epocasEjecutadas);
            }
            if (graficaPesos != null)
            {
                if (_data.modo == "3clases" && _data.perceptron1 != null)
                    graficaPesos.Dibujar3Clases(_data.perceptron1.pesos, _data.perceptron2?.pesos, _data.perceptron3?.pesos);
                else if (_data.pesos != null)
                    graficaPesos.Dibujar(_data.pesos);
            }
        }

        private static bool DataConvergio(SimuladorData d)
        {
            if (d == null) return false;
            if (d.modo == "3clases") return d.perceptron1?.convergio == true && d.perceptron2?.convergio == true && d.perceptron3?.convergio == true;
            return d.convergio;
        }

        private static int EpocasEjecutadas(SimuladorData d)
        {
            if (d == null) return 0;
            if (d.modo == "3clases" && d.perceptron1 != null)
                return Mathf.Max(d.perceptron1.epocasEjecutadas, d.perceptron2?.epocasEjecutadas ?? 0, d.perceptron3?.epocasEjecutadas ?? 0);
            return d.epocasEjecutadas;
        }

        private void OnEvaluar()
        {
            if (!_entrenado || _data == null || !JsonLoader.TieneEntrenamiento(_data))
            {
                if (textResultadoEvaluacion != null) textResultadoEvaluacion.text = "Entrene primero.";
                return;
            }
            if (pythonBridge == null) { SetStatus("Error: no hay PythonBridge."); return; }
            float x1 = ParseFloat(inputX1, 0f);
            float x2 = ParseFloat(inputX2, 0f);
            float x3 = ParseFloat(inputX3, 0f);
            int clase = pythonBridge.Evaluar(x1, x2, x3);
            if (textResultadoEvaluacion != null) textResultadoEvaluacion.text = "Clase predicha: " + clase;
            if (puntosRenderer != null) puntosRenderer.AgregarPuntoEvaluacion(x1, x2, x3, clase);
        }
    }
}
