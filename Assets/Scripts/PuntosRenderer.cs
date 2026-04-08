using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using TMPro;

namespace PerceptronSimulator
{
    /// <summary>
    /// Dibuja cada punto como esfera en (x1, x2, x3).
    /// Ejes X/Y/Z en color rojo/verde/azul con marcas numéricas según min/max de los datos (estilo matplotlib).
    /// </summary>
    public class PuntosRenderer : MonoBehaviour
    {
        [Header("Contenedores")]
        public Transform pointsContainer;
        public Transform axesContainer;

        [Header("Prefab y escala")]
        public GameObject pointPrefab;
        [Tooltip("Factor para escalar coordenadas (ej. 0.1 = dividir por 10).")]
        public float dataScale = 0.1f;
        [Tooltip("Escala de cada esfera.")]
        public float pointScale = 0.08f;
        [Tooltip("Máximo de puntos a dibujar (rendimiento).")]
        public int maxPuntos = 500;

        [Header("Colores")]
        public Color colorClase1 = Color.red;
        public Color colorClaseMenos1 = Color.blue;
        public Color colorClase0 = Color.green;
        public Color axisColorX = Color.red;
        public Color axisColorY = Color.green;
        public Color axisColorZ = Color.blue;

        [Header("Ejes (fallback si no hay puntos)")]
        public float axisLength = 8f;
        public float axisLineWidth = 0.02f;

        [Header("Etiquetas numéricas (TextMeshPro)")]
        [Range(5, 8)]
        [Tooltip("Cantidad de valores mostrados a lo largo de cada eje (incluye extremos).")]
        public int axisTickCount = 5;
        [Tooltip("Fuente TMP de las marcas (1, 2, 3.5…). Si pones 0, el código usa mínimo 2 (TMP con 0 se ve mal o gigante).")]
        public float axisTickFontSize = 8f;
        [Tooltip("Escala del transform de las marcas en el mundo (lo principal para que no se vean gigantes).")]
        public float axisTickMeshScale = 0.055f;
        [Tooltip("Fuente TMP del título X1/X2/X3 + rango.")]
        public float axisTitleFontSize = 10f;
        [Tooltip("Escala del transform del título en el mundo.")]
        public float axisTitleMeshScale = 0.078f;
        [Tooltip("Factor para separar un poco marcas/títulos del eje.")]
        public float axisLabelScale = 1f;
        [Tooltip("Giro local extra tras mirar a la cámara (marcas). Si siguen al revés, prueba (180,0,0) o (0,0,180).")]
        public Vector3 axisTickBillboardExtraEuler = new Vector3(0f, 180f, 0f);
        [Tooltip("Giro local extra tras mirar a la cámara (títulos X1/X2/X3).")]
        public Vector3 axisTitleBillboardExtraEuler = new Vector3(0f, 180f, 0f);
        [Range(0f, 0.5f)]
        [Tooltip("Grosor del borde en marcas numéricas (negro).")]
        public float axisTickOutlineWidth = 0.42f;
        [Range(0f, 0.5f)]
        [Tooltip("Grosor del borde de color en títulos X1/X2/X3.")]
        public float axisTitleOutlineWidth = 0.32f;
        [Tooltip("Las marcas numéricas en blanco+negro (recomendado); si no, usan el color del eje.")]
        public bool tickLabelsHighContrast = true;
        [Tooltip("En el Editor, oculta solo las etiquetas TMP en la vista Scene (siguen viéndose en Game / Play).")]
        public bool hideAxisLabelsInSceneView = true;
        [Header("Atajo teclado")]
        [Tooltip("Pulsa esta tecla para ocultar/mostrar marcas numéricas y títulos X1/X2/X3 (las líneas de eje siguen visibles).")]
        public KeyCode toggleAxisLabelsKey = KeyCode.W;
        [Tooltip("Si al dibujar ejes las etiquetas empiezan visibles.")]
        public bool axisLabelsStartVisible = true;
        public string axisNameX = "X1";
        public string axisNameY = "X2";
        public string axisNameZ = "X3";

        private readonly List<GameObject> _spheres = new List<GameObject>();
        private readonly List<GameObject> _axisObjects = new List<GameObject>();
        private static GameObject _defaultPointPrefab;

        /// <summary>Padre de todas las marcas y títulos TMP; se activa/desactiva con la tecla configurada.</summary>
        private Transform _axisLabelsRoot;
        private bool _axisLabelsVisible = true;

        private bool _boundsValid;
        private float _minX1, _maxX1, _minX2, _maxX2, _minX3, _maxX3;

        private static TMP_FontAsset _cachedAxisFont;

        private void Awake()
        {
            if (pointsContainer == null)
            {
                var t = transform.Find("PointsContainer");
                if (t != null) pointsContainer = t;
            }
            if (axesContainer == null)
            {
                var t = transform.Find("AxesContainer");
                if (t != null) axesContainer = t;
            }
        }

        /// <summary>
        /// Asigna fuente TMP sin depender solo de TMP_Settings (evita null si el importador falló).
        /// </summary>
        private static void AssignAxisLabelFont(TMP_Text tmp)
        {
            if (tmp == null) return;
            if (_cachedAxisFont == null)
            {
                try
                {
                    _cachedAxisFont = TMP_Settings.defaultFontAsset;
                }
                catch
                {
                    _cachedAxisFont = null;
                }
                if (_cachedAxisFont == null)
                    _cachedAxisFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            }
            if (_cachedAxisFont != null)
                tmp.font = _cachedAxisFont;
        }

        public void Clear()
        {
            foreach (var s in _spheres)
            {
                if (s != null) Destroy(s);
            }
            _spheres.Clear();
            foreach (var a in _axisObjects)
            {
                if (a != null) Destroy(a);
            }
            _axisObjects.Clear();
            _axisLabelsRoot = null;
            _boundsValid = false;
        }

        private void Update()
        {
            if (toggleAxisLabelsKey == KeyCode.None || _axisLabelsRoot == null)
                return;
            if (Input.GetKeyDown(toggleAxisLabelsKey))
            {
                _axisLabelsVisible = !_axisLabelsVisible;
                _axisLabelsRoot.gameObject.SetActive(_axisLabelsVisible);
            }
        }

        public void DibujarPuntos(List<PuntoData> puntos)
        {
            if (puntos == null) return;
            Clear();
            ComputeBoundsFromAllPoints(puntos);
            Transform parent = pointsContainer != null ? pointsContainer : transform;
            GameObject prefab = pointPrefab != null ? pointPrefab : GetOrCreateDefaultPrefab();
            int n = Mathf.Min(puntos.Count, maxPuntos);
            for (int i = 0; i < n; i++)
            {
                PuntoData p = puntos[i];
                Vector3 pos = new Vector3(p.x1, p.x2, p.x3) * dataScale;
                GameObject go = Instantiate(prefab, parent);
                go.SetActive(true);
                go.transform.localPosition = pos;
                go.transform.localScale = Vector3.one * pointScale;
                Color c = ClaseToColor(p.clase);
                ApplyColor(go, c);
                _spheres.Add(go);
            }
        }

        public void AgregarPuntoEvaluacion(float x1, float x2, float x3, int clase)
        {
            Transform parent = pointsContainer != null ? pointsContainer : transform;
            GameObject prefab = pointPrefab != null ? pointPrefab : GetOrCreateDefaultPrefab();
            Vector3 pos = new Vector3(x1, x2, x3) * dataScale;
            GameObject go = Instantiate(prefab, parent);
            go.SetActive(true);
            go.transform.localPosition = pos;
            go.transform.localScale = Vector3.one * pointScale * 1.2f;
            ApplyColor(go, ClaseToColor(clase));
            _spheres.Add(go);
        }

        public void DibujarEjes()
        {
            Transform parent = axesContainer != null ? axesContainer : transform;
            if (_boundsValid)
                DibujarEjesDesdeDatos(parent);
            else
                DibujarEjesFallback(parent);
        }

        private void ComputeBoundsFromAllPoints(List<PuntoData> puntos)
        {
            if (puntos == null || puntos.Count == 0)
            {
                _boundsValid = false;
                return;
            }

            _minX1 = _maxX1 = puntos[0].x1;
            _minX2 = _maxX2 = puntos[0].x2;
            _minX3 = _maxX3 = puntos[0].x3;
            for (int i = 1; i < puntos.Count; i++)
            {
                PuntoData p = puntos[i];
                if (p.x1 < _minX1) _minX1 = p.x1;
                if (p.x1 > _maxX1) _maxX1 = p.x1;
                if (p.x2 < _minX2) _minX2 = p.x2;
                if (p.x2 > _maxX2) _maxX2 = p.x2;
                if (p.x3 < _minX3) _minX3 = p.x3;
                if (p.x3 > _maxX3) _maxX3 = p.x3;
            }

            ExpandIfDegenerate(ref _minX1, ref _maxX1);
            ExpandIfDegenerate(ref _minX2, ref _maxX2);
            ExpandIfDegenerate(ref _minX3, ref _maxX3);
            _boundsValid = true;
        }

        private static void ExpandIfDegenerate(ref float min, ref float max)
        {
            float span = max - min;
            if (span < 1e-5f)
            {
                float c = (min + max) * 0.5f;
                min = c - 0.5f;
                max = c + 0.5f;
            }
        }

        private void DibujarEjesDesdeDatos(Transform parent)
        {
            float s = dataScale;
            Vector3 corner = new Vector3(_minX1 * s, _minX2 * s, _minX3 * s);
            float lenX = (_maxX1 - _minX1) * s;
            float lenY = (_maxX2 - _minX2) * s;
            float lenZ = (_maxX3 - _minX3) * s;

            Vector3 endX = corner + Vector3.right * lenX;
            Vector3 endY = corner + Vector3.up * lenY;
            Vector3 endZ = corner + Vector3.forward * lenZ;

            DrawAxisLine(parent, "EjeX", corner, endX, axisColorX);
            DrawAxisLine(parent, "EjeY", corner, endY, axisColorY);
            DrawAxisLine(parent, "EjeZ", corner, endZ, axisColorZ);

            GameObject labelsGo = new GameObject("AxisLabels");
            labelsGo.transform.SetParent(parent, false);
            labelsGo.transform.localPosition = Vector3.zero;
            labelsGo.transform.localRotation = Quaternion.identity;
            labelsGo.transform.localScale = Vector3.one;
            _axisLabelsRoot = labelsGo.transform;
            _axisLabelsVisible = axisLabelsStartVisible;
            labelsGo.SetActive(_axisLabelsVisible);
            _axisObjects.Add(labelsGo);

            Transform labelsParent = _axisLabelsRoot;
            int ticks = Mathf.Clamp(axisTickCount, 5, 8);
            PlaceAxisTicks(labelsParent, corner, Vector3.right, lenX, _minX1, _maxX1, ticks, axisColorX, "tickX");
            PlaceAxisTicks(labelsParent, corner, Vector3.up, lenY, _minX2, _maxX2, ticks, axisColorY, "tickY");
            PlaceAxisTicks(labelsParent, corner, Vector3.forward, lenZ, _minX3, _maxX3, ticks, axisColorZ, "tickZ");

            float titlePad = Mathf.Max(0.05f, axisLabelScale * 2.8f * axisTitleMeshScale);
            CreateAxisTitle(labelsParent, endX + Vector3.right * titlePad,
                $"{axisNameX}\n{FormatAxisNumber(_minX1)} → {FormatAxisNumber(_maxX1)}", axisColorX);
            CreateAxisTitle(labelsParent, endY + Vector3.up * titlePad,
                $"{axisNameY}\n{FormatAxisNumber(_minX2)} → {FormatAxisNumber(_maxX2)}", axisColorY);
            CreateAxisTitle(labelsParent, endZ + Vector3.forward * titlePad,
                $"{axisNameZ}\n{FormatAxisNumber(_minX3)} → {FormatAxisNumber(_maxX3)}", axisColorZ);
        }

        private void DibujarEjesFallback(Transform parent)
        {
            DrawAxisLine(parent, "EjeX", Vector3.zero, Vector3.right * axisLength, axisColorX);
            DrawAxisLine(parent, "EjeY", Vector3.zero, Vector3.up * axisLength, axisColorY);
            DrawAxisLine(parent, "EjeZ", Vector3.zero, Vector3.forward * axisLength, axisColorZ);
        }

        private void PlaceAxisTicks(Transform parent, Vector3 origin, Vector3 unitWorld, float length,
            float minVal, float maxVal, int tickCount, Color color, string prefix)
        {
            if (tickCount < 2 || length <= 1e-8f) return;
            for (int i = 0; i < tickCount; i++)
            {
                float t = tickCount == 1 ? 0f : i / (float)(tickCount - 1);
                float value = Mathf.Lerp(minVal, maxVal, t);
                Vector3 pos = origin + unitWorld * (length * t);
                string text = FormatAxisNumber(value);
                CreateAxisTickLabel(parent, pos, text, color, tickLabelsHighContrast, $"{prefix}_{i}");
            }
        }

        private static string FormatAxisNumber(float v)
        {
            var inv = CultureInfo.InvariantCulture;
            // Enteros legibles (ej. 8 en lugar de 8.00)
            if (Mathf.Abs(v - Mathf.Round(v)) < 0.0005f)
                return Mathf.RoundToInt(v).ToString(inv);

            float a = Mathf.Abs(v);
            if (a >= 100f) return v.ToString("0", inv);
            if (a >= 10f) return v.ToString("0.#", inv);
            if (a >= 1f) return v.ToString("0.##", inv);
            if (a >= 0.01f) return v.ToString("0.###", inv);
            return v.ToString("G3", inv);
        }

        private void CreateAxisTickLabel(Transform parent, Vector3 localPos, string text, Color axisColor, bool highContrast, string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            float mesh = Mathf.Max(0.002f, axisTickMeshScale) * axisLabelScale;
            go.transform.localPosition = localPos + Vector3.up * (mesh * 0.25f);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one * mesh;

            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = Mathf.Max(2f, axisTickFontSize);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.fontStyle = FontStyles.Bold;
            tmp.enableAutoSizing = false;
            AssignAxisLabelFont(tmp);
            float outline = Mathf.Clamp(axisTickOutlineWidth * (mesh * 8f), 0.04f, 0.42f);
            if (highContrast)
            {
                tmp.color = Color.white;
                tmp.outlineColor = Color.black;
                tmp.outlineWidth = outline;
            }
            else
            {
                tmp.color = axisColor;
                tmp.outlineColor = Color.black;
                tmp.outlineWidth = outline * 0.85f;
            }
            tmp.ForceMeshUpdate(true);

            var rend = go.GetComponent<MeshRenderer>();
            if (rend != null)
            {
                rend.sortingOrder = 50;
                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            var bb = go.AddComponent<BillboardToCamera>();
            bb.extraEulerDegrees = axisTickBillboardExtraEuler;
            HideAxisLabelInSceneViewIfNeeded(go);
            // No _axisObjects: vive bajo AxisLabels; al destruir el padre basta.
        }

        private void CreateAxisTitle(Transform parent, Vector3 localPos, string title, Color axisColor)
        {
            string safeName = title.Replace('\n', '_').Replace("→", "to");
            GameObject go = new GameObject("Titulo_" + safeName);
            go.transform.SetParent(parent, false);
            float mesh = Mathf.Max(0.002f, axisTitleMeshScale) * axisLabelScale;
            go.transform.localPosition = localPos + Vector3.up * (mesh * 0.35f);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one * mesh;

            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text = title;
            tmp.fontSize = Mathf.Max(2f, axisTitleFontSize);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.fontStyle = FontStyles.Bold;
            tmp.enableAutoSizing = false;
            AssignAxisLabelFont(tmp);
            // Blanco nítido + borde del color del eje (se distingue X1/X2/X3 al leer)
            tmp.color = Color.white;
            tmp.outlineColor = axisColor;
            tmp.outlineWidth = Mathf.Clamp(axisTitleOutlineWidth * (mesh * 8f), 0.04f, 0.38f);
            tmp.ForceMeshUpdate(true);

            var rend = go.GetComponent<MeshRenderer>();
            if (rend != null)
            {
                rend.sortingOrder = 51;
                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            var bb = go.AddComponent<BillboardToCamera>();
            bb.extraEulerDegrees = axisTitleBillboardExtraEuler;
            HideAxisLabelInSceneViewIfNeeded(go);
            // No _axisObjects: vive bajo AxisLabels.
        }

        private void HideAxisLabelInSceneViewIfNeeded(GameObject go)
        {
#if UNITY_EDITOR
            if (!hideAxisLabelsInSceneView || go == null) return;
            try
            {
                UnityEditor.SceneVisibilityManager.instance.Hide(go, false);
            }
            catch
            {
                /* Editor sin soporte o API distinta */
            }
#endif
        }

        private Color ClaseToColor(int clase)
        {
            if (clase == 1) return colorClase1;
            if (clase == -1) return colorClaseMenos1;
            return colorClase0;
        }

        private void DrawAxisLine(Transform parent, string name, Vector3 from, Vector3 to, Color c)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.localPosition = Vector3.zero;
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, from);
            lr.SetPosition(1, to);
            lr.startWidth = axisLineWidth;
            lr.endWidth = axisLineWidth * 0.5f;
            lr.material = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color"));
            lr.startColor = lr.endColor = c;
            _axisObjects.Add(go);
        }

        private void ApplyColor(GameObject go, Color c)
        {
            if (go.TryGetComponent<Renderer>(out var r))
            {
                var sh = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
                var mat = sh != null ? new Material(sh) : new Material(r.sharedMaterial);
                mat.color = c;
                r.material = mat;
            }
        }

        private static GameObject GetOrCreateDefaultPrefab()
        {
            if (_defaultPointPrefab != null) return _defaultPointPrefab;
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "PuntoSfera";
            sphere.SetActive(false);
            _defaultPointPrefab = sphere;
            return sphere;
        }
    }
}
