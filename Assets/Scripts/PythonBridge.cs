using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;
// System.Diagnostics también tiene una clase Debug; sin esto, Debug.Log es ambiguo (CS0104).
using Debug = UnityEngine.Debug;

namespace PerceptronSimulator
{
    /// <summary>
    /// Llama a Python (perceptron_engine.py) para generar, entrenar y evaluar.
    /// Acciones: generar, entrenar, evaluar.
    /// </summary>
    public class PythonBridge : MonoBehaviour
    {
        [Header("Depuración")]
        [Tooltip("Si está activo, escribe en la Consola la salida de Python al terminar (útil para ver prints y errores).")]
        public bool logSalidaPythonEnConsola = false;

        private static string GetPythonPath()
        {
            string[] candidates = { "python", "python3", "py" };
            foreach (string c in candidates)
            {
                try
                {
                    var p = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = c,
                            Arguments = "--version",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
                        }
                    };
                    p.Start();
                    p.WaitForExit(2000);
                    if (p.ExitCode == 0) return c;
                }
                catch { }
            }
            return null;
        }

        private static string GetJsonPath()
        {
            string dir = Application.isEditor
                ? Path.Combine(Application.dataPath, "..")
                : Application.persistentDataPath;
            return Path.Combine(dir, "datos.json");
        }

        private static string GetScriptPath()
        {
            string inStreaming = Path.Combine(Application.streamingAssetsPath, "perceptron_engine.py");
            if (File.Exists(inStreaming)) return inStreaming;
            return Path.Combine(Application.dataPath, "..", "Python", "perceptron_engine.py");
        }

        private bool RunProcess(string exe, string arguments, out string stdOut, out string stdErr, int timeoutMs = 15000)
        {
            stdOut = stdErr = "";
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetDirectoryName(Application.dataPath) ?? Application.dataPath
                };
                using var p = Process.Start(startInfo);
                if (p == null)
                {
                    Debug.LogError("[PythonBridge] Process.Start devolvió null. No se pudo iniciar el proceso.");
                    return false;
                }
                stdOut = p.StandardOutput.ReadToEnd();
                stdErr = p.StandardError.ReadToEnd();
                p.WaitForExit(timeoutMs);
                if (p.ExitCode != 0)
                {
                    Debug.LogWarning(
                        $"[PythonBridge] Python salió con código {p.ExitCode}.\n" +
                        $"Comando: {exe} {arguments}\n" +
                        $"STDERR:\n{stdErr}\nSTDOUT:\n{stdOut}");
                    return false;
                }
                if (logSalidaPythonEnConsola && (!string.IsNullOrWhiteSpace(stdOut) || !string.IsNullOrWhiteSpace(stdErr)))
                    Debug.Log($"[PythonBridge] Salida Python — STDOUT:\n{stdOut}\nSTDERR:\n{stdErr}");
                return true;
            }
            catch (Exception ex)
            {
                stdErr = ex.Message;
                Debug.LogError($"[PythonBridge] Excepción al ejecutar Python: {ex.Message}\nComando: {exe} {arguments}");
                return false;
            }
        }

        /// <summary>Llama: python perceptron_engine.py datos.json generar 2clases m1 d1 m2 d2 instancias</summary>
        public bool Generar2Clases(float m1, float d1, float m2, float d2, int instancias)
        {
            string python = GetPythonPath();
            if (string.IsNullOrEmpty(python))
            {
                Debug.LogError("[PythonBridge] No se encontró Python en el PATH (prueba: python, python3, py). Unity a veces no ve el mismo PATH que tu terminal.");
                return false;
            }
            string scriptPath = GetScriptPath();
            if (!File.Exists(scriptPath))
            {
                Debug.LogError("[PythonBridge] No está perceptron_engine.py en: " + scriptPath);
                return false;
            }
            string jsonPath = GetJsonPath();
            string args = $"\"{scriptPath}\" \"{jsonPath}\" generar 2clases {m1.ToString(System.Globalization.CultureInfo.InvariantCulture)} {d1.ToString(System.Globalization.CultureInfo.InvariantCulture)} {m2.ToString(System.Globalization.CultureInfo.InvariantCulture)} {d2.ToString(System.Globalization.CultureInfo.InvariantCulture)} {instancias}";
            bool ok = RunProcess(python, args, out _, out _);
            if (ok && !File.Exists(jsonPath))
            {
                Debug.LogError("[PythonBridge] Python terminó bien pero no existe datos.json en: " + jsonPath);
                return false;
            }
            return ok;
        }

        /// <summary>Llama: python perceptron_engine.py datos.json generar 3clases m1 d1 m2 d2 m3 d3 instancias</summary>
        public bool Generar3Clases(float m1, float d1, float m2, float d2, float m3, float d3, int instancias)
        {
            string python = GetPythonPath();
            if (string.IsNullOrEmpty(python))
            {
                Debug.LogError("[PythonBridge] No se encontró Python en el PATH (prueba: python, python3, py).");
                return false;
            }
            string scriptPath = GetScriptPath();
            if (!File.Exists(scriptPath))
            {
                Debug.LogError("[PythonBridge] No está perceptron_engine.py en: " + scriptPath);
                return false;
            }
            string jsonPath = GetJsonPath();
            string args = $"\"{scriptPath}\" \"{jsonPath}\" generar 3clases {m1.ToString(System.Globalization.CultureInfo.InvariantCulture)} {d1.ToString(System.Globalization.CultureInfo.InvariantCulture)} {m2.ToString(System.Globalization.CultureInfo.InvariantCulture)} {d2.ToString(System.Globalization.CultureInfo.InvariantCulture)} {m3.ToString(System.Globalization.CultureInfo.InvariantCulture)} {d3.ToString(System.Globalization.CultureInfo.InvariantCulture)} {instancias}";
            bool ok = RunProcess(python, args, out _, out _);
            if (ok && !File.Exists(jsonPath))
            {
                Debug.LogError("[PythonBridge] Python terminó bien pero no existe datos.json en: " + jsonPath);
                return false;
            }
            return ok;
        }

        /// <summary>Llama: python perceptron_engine.py datos.json entrenar 2clases|3clases epocas tasa</summary>
        public bool Entrenar(string modo, int epocas, float tasa = 0.1f)
        {
            string python = GetPythonPath();
            if (string.IsNullOrEmpty(python))
            {
                Debug.LogError("[PythonBridge] No se encontró Python en el PATH.");
                return false;
            }
            string scriptPath = GetScriptPath();
            if (!File.Exists(scriptPath))
            {
                Debug.LogError("[PythonBridge] No está perceptron_engine.py en: " + scriptPath);
                return false;
            }
            string jsonPath = GetJsonPath();
            string args = $"\"{scriptPath}\" \"{jsonPath}\" entrenar {modo} {epocas} {tasa.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            bool ok = RunProcess(python, args, out _, out _);
            if (ok && !File.Exists(jsonPath))
            {
                Debug.LogError("[PythonBridge] Entrenar: no se encontró datos.json en: " + jsonPath);
                return false;
            }
            return ok;
        }

        /// <summary>Llama: python perceptron_engine.py datos.json evaluar x1 x2 x3. Retorna clase predicha: 1, -1 o 0.</summary>
        public int Evaluar(float x1, float x2, float x3)
        {
            string python = GetPythonPath();
            if (string.IsNullOrEmpty(python))
            {
                Debug.LogError("[PythonBridge] Evaluar: no hay Python en PATH.");
                return 0;
            }
            string scriptPath = GetScriptPath();
            if (!File.Exists(scriptPath))
            {
                Debug.LogError("[PythonBridge] Evaluar: no hay script en " + scriptPath);
                return 0;
            }
            string jsonPath = GetJsonPath();
            if (!File.Exists(jsonPath))
            {
                Debug.LogError("[PythonBridge] Evaluar: falta datos.json en " + jsonPath);
                return 0;
            }
            string args = $"\"{scriptPath}\" \"{jsonPath}\" evaluar {x1.ToString(System.Globalization.CultureInfo.InvariantCulture)} {x2.ToString(System.Globalization.CultureInfo.InvariantCulture)} {x3.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            bool ok = RunProcess(python, args, out string stdOut, out _);
            if (!ok) return 0;
            string trimmed = (stdOut ?? "").Trim();
            if (int.TryParse(trimmed, out int clase))
                return clase;
            return 0;
        }

        /// <summary>Ruta al archivo datos.json que usa el bridge.</summary>
        public static string RutaJson() => GetJsonPath();
    }
}
