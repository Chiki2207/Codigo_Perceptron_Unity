using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

namespace PerceptronSimulator
{
    [Serializable]
    public class PuntoData
    {
        public float x1, x2, x3;
        public int clase;
    }

    [Serializable]
    public class PesoEpoca
    {
        public List<float> pesos;
        public float bias;
    }

    [Serializable]
    public class PerceptronData
    {
        public List<float> errores;
        public List<PesoEpoca> pesos;
        public List<float> pesosFinales;
        public float biasFinal;
        public int epocasEjecutadas;
        public bool convergio;
    }

    [Serializable]
    public class SimuladorData
    {
        public string modo;
        public List<PuntoData> puntos;
        // Para 2 clases:
        public List<float> errores;
        public List<PesoEpoca> pesos;
        public List<float> pesosFinales;
        public float biasFinal;
        public int epocasEjecutadas;
        public float tasaUsada;
        public bool convergio;
        // Para 3 clases:
        public PerceptronData perceptron1;
        public PerceptronData perceptron2;
        public PerceptronData perceptron3;
    }

    /// <summary>
    /// Lee y parsea datos.json generado por el motor Python.
    /// </summary>
    public static class JsonLoader
    {
        public static bool Cargar(string path, out SimuladorData data, out string error)
        {
            data = null;
            error = null;
            if (!File.Exists(path))
            {
                error = "No se encontró: " + path;
                return false;
            }
            try
            {
                string json = File.ReadAllText(path);
                data = JsonConvert.DeserializeObject<SimuladorData>(json);
                return data != null;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static bool TieneEntrenamiento(SimuladorData data)
        {
            if (data == null) return false;
            if (data.modo == "3clases")
                return data.perceptron1 != null && data.perceptron1.pesosFinales != null;
            return data.pesosFinales != null && data.pesosFinales.Count >= 3;
        }
    }
}
