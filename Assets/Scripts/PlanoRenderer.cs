using System.Collections.Generic;
using UnityEngine;

namespace PerceptronSimulator
{
    public class PlanoRenderer : MonoBehaviour
    {
        [Header("Contenedor y escala")]
        public Transform planeContainer;
        [Tooltip("Mismo factor que PuntosRenderer (para alinear con los puntos).")]
        public float dataScale = 0.1f;

        [Header("Guía visual")]
        [Tooltip("Líneas rojas en cruz sobre el plano (frontera de decisión). Desactivado por defecto.")]
        public bool drawSeparationLine = false;
        public Color separationLineColor = new Color(1f, 0f, 0f, 1f);
        public float separationLineWidth = 0.04f;

        [Header("Colores")]
        public Color colorPlano2Clases = new Color(1f, 1f, 0.3f, 0.45f);
        public Color colorPlano1 = new Color(1f, 0.3f, 0.3f, 0.45f);
        public Color colorPlano2 = new Color(0.3f, 0.3f, 1f, 0.45f);
        public Color colorPlano3 = new Color(0.3f, 0.8f, 0.3f, 0.45f);

        [Header("Colores de clases (bicolor en 3 clases)")]
        public Color colorClase1     = new Color(1f,  0.3f, 0.3f, 0.45f);
        public Color colorClaseMenos1 = new Color(0.3f, 0.3f, 1f,  0.45f);
        public Color colorClase0     = new Color(0.3f, 0.8f, 0.3f, 0.45f);

        [Header("Borde del plano")]
        public Color borderColor = new Color(1f, 0.85f, 0f, 1f);
        public float borderWidth = 0.025f;

        private readonly List<GameObject> _planes = new List<GameObject>();
        private Vector3 _dataCentroid;
        private float _dataRadius;
        private readonly List<Vector3> _dataPointsWorld = new List<Vector3>();

        public void Clear()
        {
            foreach (var p in _planes)
                if (p != null) Destroy(p);
            _planes.Clear();
            _dataPointsWorld.Clear();
        }

        public void DibujarDesdeSimulador(SimuladorData data)
        {
            Clear();
            if (data == null || !JsonLoader.TieneEntrenamiento(data)) return;

            ComputeDataBounds(data.puntos);

            if (data.modo == "3clases")
            {
                if (data.perceptron1?.pesosFinales != null)
                    DibujarPlano(data.perceptron1.pesosFinales, data.perceptron1.biasFinal, colorClase1, colorClaseMenos1);
                if (data.perceptron2?.pesosFinales != null)
                    DibujarPlano(data.perceptron2.pesosFinales, data.perceptron2.biasFinal, colorClase1, colorClase0);
                if (data.perceptron3?.pesosFinales != null)
                    DibujarPlano(data.perceptron3.pesosFinales, data.perceptron3.biasFinal, colorClaseMenos1, colorClase0);
            }
            else
            {
                if (data.pesosFinales != null)
                    DibujarPlano(data.pesosFinales, data.biasFinal, colorPlano2Clases);
            }
        }

        public void DibujarPlano(List<float> pesosFinales, float biasFinal, Color? color = null)
        {
            if (pesosFinales == null || pesosFinales.Count < 3) return;
            DibujarPlano(pesosFinales[0], pesosFinales[1], pesosFinales[2], biasFinal, color ?? colorPlano2Clases);
        }

        public void DibujarPlano(List<float> pesosFinales, float biasFinal, Color colorA, Color colorB)
        {
            if (pesosFinales == null || pesosFinales.Count < 3) return;
            DibujarPlano(pesosFinales[0], pesosFinales[1], pesosFinales[2], biasFinal, colorA, colorB);
        }

        public void DibujarPlano(float w1, float w2, float w3, float bias, Color colorA, Color colorB)
        {
            Vector3 wVec = new Vector3(w1, w2, w3);
            float mag = wVec.magnitude;
            if (mag < 1e-6f) return;
            Vector3 normal = wVec / mag;

            float biasWorld = bias * dataScale;
            float signedDist = (Vector3.Dot(wVec, _dataCentroid) + biasWorld) / mag;
            Vector3 center = _dataCentroid - normal * signedDist;

            Vector3 tangent1 = Vector3.Cross(normal, Vector3.up);
            if (tangent1.sqrMagnitude < 1e-4f)
                tangent1 = Vector3.Cross(normal, Vector3.right);
            tangent1.Normalize();
            Vector3 tangent2 = Vector3.Cross(normal, tangent1).normalized;

            float size = Mathf.Max(0.5f, _dataRadius * 2.2f);
            if (_dataPointsWorld.Count > 0)
            {
                float minU = float.PositiveInfinity, maxU = float.NegativeInfinity;
                float minV = float.PositiveInfinity, maxV = float.NegativeInfinity;
                foreach (var p in _dataPointsWorld)
                {
                    float u = Vector3.Dot(p, tangent1);
                    float v = Vector3.Dot(p, tangent2);
                    if (u < minU) minU = u; if (u > maxU) maxU = u;
                    if (v < minV) minV = v; if (v > maxV) maxV = v;
                }
                size = Mathf.Max(0.5f, Mathf.Max(maxU - minU, maxV - minV) * 1.15f);
            }

            Transform t = planeContainer != null ? planeContainer : transform;
            Vector3 centerWorld     = t.TransformPoint(center);
            Vector3 normalWorld     = t.TransformVector(normal).normalized;
            Vector3 tangent1WorldVec = t.TransformVector(tangent1);
            Vector3 tangent2WorldVec = t.TransformVector(tangent2);
            float sizeScale         = Mathf.Max(tangent1WorldVec.magnitude, tangent2WorldVec.magnitude);
            float sizeWorld         = size * sizeScale;
            Vector3 t1Dir = tangent1WorldVec.sqrMagnitude > 1e-8f ? tangent1WorldVec.normalized : Vector3.right;
            Vector3 t2Dir = tangent2WorldVec.sqrMagnitude > 1e-8f ? tangent2WorldVec.normalized : Vector3.up;

            float halfSize  = sizeWorld * 0.5f;
            float quarterSize = sizeWorld * 0.25f;

            // Mitad izquierda → colorA
            CrearMitadQuad("Plano_A",      centerWorld - t1Dir * quarterSize,  normalWorld, t2Dir, halfSize, sizeWorld, colorA);
            CrearMitadQuad("PlanoBack_A",  centerWorld - t1Dir * quarterSize, -normalWorld, t2Dir, halfSize, sizeWorld, colorA);
            // Mitad derecha → colorB
            CrearMitadQuad("Plano_B",      centerWorld + t1Dir * quarterSize,  normalWorld, t2Dir, halfSize, sizeWorld, colorB);
            CrearMitadQuad("PlanoBack_B",  centerWorld + t1Dir * quarterSize, -normalWorld, t2Dir, halfSize, sizeWorld, colorB);

            // Borde exterior (igual que siempre)
            float h = sizeWorld * 0.5f;
            Vector3 c0 = centerWorld - t1Dir * h - t2Dir * h;
            Vector3 c1 = centerWorld + t1Dir * h - t2Dir * h;
            Vector3 c2 = centerWorld + t1Dir * h + t2Dir * h;
            Vector3 c3 = centerWorld - t1Dir * h + t2Dir * h;
            DrawWorldLoop("Borde", new[] { c0, c1, c2, c3, c0 }, borderColor, borderWidth);

            if (drawSeparationLine)
            {
                float halfLen = sizeWorld * 0.55f;
                DrawWorldLine("SepLinea1", centerWorld - t1Dir * halfLen, centerWorld + t1Dir * halfLen, separationLineColor);
                DrawWorldLine("SepLinea2", centerWorld - t2Dir * halfLen, centerWorld + t2Dir * halfLen, separationLineColor);
            }
        }

        private void CrearMitadQuad(string nombre, Vector3 posicion, Vector3 normalW, Vector3 t2DirW, float anchoW, float altoW, Color color)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = nombre;
            quad.transform.position = posicion;
            quad.transform.rotation = Quaternion.LookRotation(normalW, t2DirW);
            quad.transform.localScale = new Vector3(anchoW, altoW, 1f);
            SetupQuadRenderer(quad, color);
            _planes.Add(quad);
        }

        public void DibujarPlano(float w1, float w2, float w3, float bias, Color? color = null)
        {
            Color c = color ?? colorPlano2Clases;

            Vector3 wVec = new Vector3(w1, w2, w3);
            float mag = wVec.magnitude;
            if (mag < 1e-6f) return;
            Vector3 normal = wVec / mag;

            // Position: project data centroid onto the mathematical plane
            float biasWorld = bias * dataScale;
            float signedDist = (Vector3.Dot(wVec, _dataCentroid) + biasWorld) / mag;
            Vector3 center = _dataCentroid - normal * signedDist;

            // Two tangent directions lying on the plane surface
            Vector3 tangent1 = Vector3.Cross(normal, Vector3.up);
            if (tangent1.sqrMagnitude < 1e-4f)
                tangent1 = Vector3.Cross(normal, Vector3.right);
            tangent1.Normalize();
            Vector3 tangent2 = Vector3.Cross(normal, tangent1).normalized;

            // Size: compute actual bounds in the plane basis (prevents the plane from being "too short")
            float size = Mathf.Max(0.5f, _dataRadius * 2.2f);
            if (_dataPointsWorld.Count > 0)
            {
                float minU = float.PositiveInfinity;
                float maxU = float.NegativeInfinity;
                float minV = float.PositiveInfinity;
                float maxV = float.NegativeInfinity;

                foreach (var p in _dataPointsWorld)
                {
                    float u = Vector3.Dot(p, tangent1);
                    float v = Vector3.Dot(p, tangent2);
                    if (u < minU) minU = u;
                    if (u > maxU) maxU = u;
                    if (v < minV) minV = v;
                    if (v > maxV) maxV = v;
                }

                float sizeU = maxU - minU;
                float sizeV = maxV - minV;
                float maxSide = Mathf.Max(sizeU, sizeV);
                size = Mathf.Max(0.5f, maxSide * 1.15f); // margin so it covers points visually
            }

            // Front quad
            Transform t = planeContainer != null ? planeContainer : transform;
            Vector3 centerWorld = t.TransformPoint(center);
            Vector3 normalWorld = t.TransformVector(normal).normalized;
            Vector3 tangent1WorldVec = t.TransformVector(tangent1);
            Vector3 tangent2WorldVec = t.TransformVector(tangent2);
            float sizeScale = Mathf.Max(tangent1WorldVec.magnitude, tangent2WorldVec.magnitude);
            float sizeWorld = size * sizeScale;
            Vector3 tangent1DirWorld = tangent1WorldVec.sqrMagnitude > 1e-8f ? tangent1WorldVec.normalized : Vector3.right;
            Vector3 tangent2DirWorld = tangent2WorldVec.sqrMagnitude > 1e-8f ? tangent2WorldVec.normalized : Vector3.up;
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "PlanoSeparacion";
            quad.transform.position = centerWorld;
            quad.transform.rotation = Quaternion.LookRotation(normalWorld, tangent2DirWorld);
            quad.transform.localScale = new Vector3(sizeWorld, sizeWorld, 1f);
            SetupQuadRenderer(quad, c);
            _planes.Add(quad);

            // Back quad (visible from behind)
            GameObject quadBack = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quadBack.name = "PlanoBack";
            quadBack.transform.position = centerWorld;
            quadBack.transform.rotation = Quaternion.LookRotation(-normalWorld, tangent2DirWorld);
            quadBack.transform.localScale = new Vector3(sizeWorld, sizeWorld, 1f);
            SetupQuadRenderer(quadBack, c);
            _planes.Add(quadBack);

            // Border rectangle
            float h = sizeWorld * 0.5f;
            Vector3 c0 = centerWorld - tangent1DirWorld * h - tangent2DirWorld * h;
            Vector3 c1 = centerWorld + tangent1DirWorld * h - tangent2DirWorld * h;
            Vector3 c2 = centerWorld + tangent1DirWorld * h + tangent2DirWorld * h;
            Vector3 c3 = centerWorld - tangent1DirWorld * h + tangent2DirWorld * h;
            DrawWorldLoop("Borde", new[] { c0, c1, c2, c3, c0 }, borderColor, borderWidth);

            // Separation cross lines
            if (drawSeparationLine)
            {
                float halfLen = sizeWorld * 0.55f;
                DrawWorldLine("SepLinea1",
                    centerWorld - tangent1DirWorld * halfLen, centerWorld + tangent1DirWorld * halfLen, separationLineColor);
                DrawWorldLine("SepLinea2",
                    centerWorld - tangent2DirWorld * halfLen, centerWorld + tangent2DirWorld * halfLen, separationLineColor);
            }
        }

        private void ComputeDataBounds(List<PuntoData> puntos)
        {
            _dataCentroid = Vector3.zero;
            _dataRadius = 1f;
            if (puntos == null || puntos.Count == 0) return;

            _dataPointsWorld.Clear();
            Vector3 sum = Vector3.zero;
            foreach (var p in puntos)
            {
                var world = new Vector3(p.x1, p.x2, p.x3) * dataScale;
                _dataPointsWorld.Add(world);
                sum += world;
            }
            _dataCentroid = sum / _dataPointsWorld.Count;

            float maxDist = 0f;
            foreach (var wp in _dataPointsWorld)
            {
                float dist = (wp - _dataCentroid).magnitude;
                if (dist > maxDist) maxDist = dist;
            }
            _dataRadius = Mathf.Max(0.5f, maxDist);
        }

        private static void SetupQuadRenderer(GameObject quad, Color c)
        {
            var col = quad.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var rend = quad.GetComponent<Renderer>();
            if (rend == null) return;

            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
            var mat = new Material(shader);
            mat.color = c;
            mat.renderQueue = 3000;
            rend.material = mat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
        }

        private void DrawWorldLine(string name, Vector3 a, Vector3 b, Color c)
        {
            var go = new GameObject(name);
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            lr.SetPosition(0, a);
            lr.SetPosition(1, b);
            lr.startWidth = separationLineWidth;
            lr.endWidth = separationLineWidth;
            var sh = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
            lr.material = new Material(sh);
            lr.startColor = lr.endColor = c;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _planes.Add(go);
        }

        private void DrawWorldLoop(string name, Vector3[] pts, Color c, float width)
        {
            var go = new GameObject(name);
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = pts.Length;
            lr.useWorldSpace = true;
            for (int i = 0; i < pts.Length; i++)
                lr.SetPosition(i, pts[i]);
            lr.loop = false;
            lr.startWidth = width;
            lr.endWidth = width;
            var sh = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
            lr.material = new Material(sh);
            lr.startColor = lr.endColor = c;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _planes.Add(go);
        }
    }
}
