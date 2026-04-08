
import numpy as np
import json
import sys

def generar_datos_2_clases(media1, desviacion1, media2, desviacion2, instancias):
    desviacion1 = max(0.0001, desviacion1)
    desviacion2 = max(0.0001, desviacion2)
    n = max(2, instancias // 2)
    clase1 = np.random.normal(media1, desviacion1, (n, 3))
    clase2 = np.random.normal(media2, desviacion2, (n, 3))
    etiquetas1 = np.ones(n)
    etiquetas2 = -np.ones(n)
    datos = np.vstack([clase1, clase2])
    clases = np.hstack([etiquetas1, etiquetas2])
    return datos, clases


def generar_datos_3_clases(media1, desv1, media2, desv2, media3, desv3, instancias):
    desv1 = max(0.0001, desv1)
    desv2 = max(0.0001, desv2)
    desv3 = max(0.0001, desv3)
    n = max(2, instancias // 3)
    clase1 = np.random.normal(media1, desv1, (n, 3))
    clase2 = np.random.normal(media2, desv2, (n, 3))
    clase3 = np.random.normal(media3, desv3, (n, 3))
    datos = np.vstack([clase1, clase2, clase3])
    clases = np.hstack([np.ones(n), -np.ones(n), np.zeros(n)])
    return datos, clases


def entrenar_perceptron(datos, clases, epocas, tasa=0.1, clase_objetivo=1):
    y_bin = np.where(clases == clase_objetivo, 1, -1)

    pesos = np.random.rand(3)
    bias = float(np.random.uniform(0.0, 0.2))

    historial_errores = []
    historial_pesos = []

    for epoca in range(epocas):
        error_total = 0
        for xi, yi in zip(datos, y_bin):
            suma = np.dot(xi, pesos) + bias
            prediccion = 1 if suma > 0 else -1
            error = yi - prediccion
            error_total += abs(error)
            pesos += tasa * error * xi
            bias += tasa * error
        historial_errores.append(float(error_total))
        historial_pesos.append({"pesos": pesos.copy().tolist(), "bias": float(bias)})
        if error_total == 0:
            break

    return pesos, bias, historial_errores, historial_pesos


def predecir_2_clases(x, pesos, bias):
    
    suma = np.dot(x, pesos) + bias
    return 1 if suma > 0 else -1


def predecir_3_clases(x, pesos1, bias1, pesos2, bias2, pesos3, bias3):
    """
    Predice clase 1, -1 o 0 usando votación uno-contra-uno.
    P1 (clase 1 vs -1), P2 (clase 1 vs 0), P3 (clase -1 vs 0).
    """
    s1 = np.dot(x, pesos1) + bias1
    s2 = np.dot(x, pesos2) + bias2
    s3 = np.dot(x, pesos3) + bias3

    voto_p1 = 1 if s1 > 0 else -1
    voto_p2 = 1 if s2 > 0 else 0
    voto_p3 = -1 if s3 > 0 else 0

    votos = {1: 0, -1: 0, 0: 0}
    votos[voto_p1] += 1
    votos[voto_p2] += 1
    votos[voto_p3] += 1

    if votos[1] == votos[-1] == votos[0]:  # empate total 1-1-1
        conf_p1 = abs(s1)
        conf_p2 = abs(s2)
        conf_p3 = abs(s3)
        max_conf = max(conf_p1, conf_p2, conf_p3)
        if max_conf == conf_p1:
            return voto_p1
        elif max_conf == conf_p2:
            return voto_p2
        else:
            return voto_p3
    else:
        return max(votos, key=votos.get)


def exportar_solo_puntos(ruta, modo, datos, clases):
    """Exporta solo los puntos (datos fijos) para que Unity los muestre. Luego 'entrenar' leerá este archivo."""
    resultado = {
        "modo": modo,
        "puntos": [
            {"x1": float(d[0]), "x2": float(d[1]), "x3": float(d[2]), "clase": int(c)}
            for d, c in zip(datos, clases)
        ],
    }
    with open(ruta, "w", encoding="utf-8") as f:
        json.dump(resultado, f, indent=0)


def exportar_json_2_clases(ruta, datos, clases, errores, pesos_historia, pesos_finales, bias_final, tasa_usada=0.1):
    """Exporta resultado de 2 clases a JSON para Unity."""
    resultado = {
        "modo": "2clases",
        "puntos": [
            {"x1": float(d[0]), "x2": float(d[1]), "x3": float(d[2]), "clase": int(c)}
            for d, c in zip(datos, clases)
        ],
        "errores": errores,
        "pesos": pesos_historia,
        "pesosFinales": pesos_finales.tolist(),
        "biasFinal": float(bias_final),
        "epocasEjecutadas": len(errores),
        "tasaUsada": float(tasa_usada),
        "convergio": len(errores) > 0 and errores[-1] == 0,
    }
    with open(ruta, "w", encoding="utf-8") as f:
        json.dump(resultado, f, indent=0)


def exportar_json_3_clases(
    ruta, datos, clases,
    errores1, pesos1_hist, pesos1_f, bias1_f,
    errores2, pesos2_hist, pesos2_f, bias2_f,
    errores3, pesos3_hist, pesos3_f, bias3_f,
    tasa_usada=0.1,
):
    """Exporta resultado de 3 clases (3 perceptrones) a JSON para Unity."""
    resultado = {
        "modo": "3clases",
        "puntos": [
            {"x1": float(d[0]), "x2": float(d[1]), "x3": float(d[2]), "clase": int(c)}
            for d, c in zip(datos, clases)
        ],
        "perceptron1": {
            "errores": errores1,
            "pesos": pesos1_hist,
            "pesosFinales": pesos1_f.tolist(),
            "biasFinal": float(bias1_f),
            "epocasEjecutadas": len(errores1),
            "convergio": len(errores1) > 0 and errores1[-1] == 0,
        },
        "perceptron2": {
            "errores": errores2,
            "pesos": pesos2_hist,
            "pesosFinales": pesos2_f.tolist(),
            "biasFinal": float(bias2_f),
            "epocasEjecutadas": len(errores2),
            "convergio": len(errores2) > 0 and errores2[-1] == 0,
        },
        "perceptron3": {
            "errores": errores3,
            "pesos": pesos3_hist,
            "pesosFinales": pesos3_f.tolist(),
            "biasFinal": float(bias3_f),
            "epocasEjecutadas": len(errores3),
            "convergio": len(errores3) > 0 and errores3[-1] == 0,
        },
        "tasaUsada": float(tasa_usada),
    }
    with open(ruta, "w", encoding="utf-8") as f:
        json.dump(resultado, f, indent=0)


def cargar_puntos_desde_json(ruta):
    """Carga solo puntos y modo desde un JSON (generado por 'generar')."""
    with open(ruta, "r", encoding="utf-8") as f:
        data = json.load(f)
    puntos = data["puntos"]
    datos = np.array([[p["x1"], p["x2"], p["x3"]] for p in puntos])
    clases = np.array([p["clase"] for p in puntos])
    modo = data.get("modo", "2clases")
    return datos, clases, modo


def evaluar_desde_json(ruta, x1, x2, x3):
    """
    Carga el JSON (debe tener pesos tras entrenar), predice la clase para (x1,x2,x3)
    y devuelve la clase (1, -1 o 0 para 3 clases).
    """
    with open(ruta, "r", encoding="utf-8") as f:
        data = json.load(f)
    if "pesosFinales" not in data and "perceptron1" not in data:
        print("ERROR: entrena primero")
        sys.exit(1)
    modo = data.get("modo", "2clases")
    x = np.array([float(x1), float(x2), float(x3)])
    if modo == "2clases":
        pesos = np.array(data["pesosFinales"])
        bias = data["biasFinal"]
        return int(predecir_2_clases(x, pesos, bias))
    else:
        p1 = data["perceptron1"]
        p2 = data["perceptron2"]
        p3 = data["perceptron3"]
        return int(predecir_3_clases(
            x,
            np.array(p1["pesosFinales"]), p1["biasFinal"],
            np.array(p2["pesosFinales"]), p2["biasFinal"],
            np.array(p3["pesosFinales"]), p3["biasFinal"],
        ))


def main():
    """Entrada por línea de comandos para que Unity pueda llamar al script.
    Uso:
      generar:  python perceptron_engine.py <ruta_json> generar 2clases media1 desv1 media2 desv2 [media3 desv3] instancias
      entrenar: python perceptron_engine.py <ruta_json> entrenar 2clases epocas
                (lee puntos del JSON y añade errores/pesos)
      Todo en uno (legacy): python ... <ruta> 2clases media1 desv1 media2 desv2 instancias epocas
    """
    if len(sys.argv) < 3:
        print("Uso: generar | entrenar | 2clases | 3clases ...")
        sys.exit(1)

    ruta_json = sys.argv[1]
    accion = sys.argv[2].lower()

    if accion == "generar":
        if len(sys.argv) < 9:
            print("generar requiere: ruta_json generar 2clases media1 desv1 media2 desv2 instancias")
            sys.exit(1)
        modo = sys.argv[3].lower()
        if modo == "2clases":
            media1, desv1 = float(sys.argv[4]), float(sys.argv[5])
            media2, desv2 = float(sys.argv[6]), float(sys.argv[7])
            instancias = int(sys.argv[8])
            if instancias < 4:
                print("ERROR: mínimo 4 instancias (2 por clase) para datos supervisados de entrenamiento")
                sys.exit(1)
            semilla = int(sys.argv[9]) if len(sys.argv) > 9 and sys.argv[9].isdigit() else None
            if semilla is not None:
                np.random.seed(semilla)
            datos, clases = generar_datos_2_clases(media1, desv1, media2, desv2, instancias)
            exportar_solo_puntos(ruta_json, "2clases", datos, clases)
        else:
            if len(sys.argv) < 11:
                print("generar 3clases: media1 desv1 media2 desv2 media3 desv3 instancias")
                sys.exit(1)
            media1, desv1 = float(sys.argv[4]), float(sys.argv[5])
            media2, desv2 = float(sys.argv[6]), float(sys.argv[7])
            media3, desv3 = float(sys.argv[8]), float(sys.argv[9])
            instancias = int(sys.argv[10])
            if instancias < 6:
                print("ERROR: mínimo 6 instancias (2 por clase) para datos supervisados de entrenamiento")
                sys.exit(1)
            semilla = int(sys.argv[11]) if len(sys.argv) > 11 and sys.argv[11].isdigit() else None
            if semilla is not None:
                np.random.seed(semilla)
            datos, clases = generar_datos_3_clases(media1, desv1, media2, desv2, media3, desv3, instancias)
            exportar_solo_puntos(ruta_json, "3clases", datos, clases)
        print("OK", ruta_json)
        return

    if accion == "entrenar":
        if len(sys.argv) < 5:
            print("entrenar requiere: ruta_json entrenar 2clases|3clases epocas [tasa]")
            sys.exit(1)
        modo = sys.argv[3].lower()
        epocas = int(sys.argv[4])
        tasa = float(sys.argv[5]) if len(sys.argv) > 5 else 0.1
        datos, clases, _ = cargar_puntos_desde_json(ruta_json)
        if modo == "2clases":
            pesos, bias, errores, pesos_hist = entrenar_perceptron(datos, clases, epocas, tasa=tasa, clase_objetivo=1)
            exportar_json_2_clases(ruta_json, datos, clases, errores, pesos_hist, pesos, bias, tasa_usada=tasa)
        else:
            mask1 = (clases == 1) | (clases == -1)
            p1, b1, e1, ph1 = entrenar_perceptron(datos[mask1], clases[mask1], epocas, tasa=tasa, clase_objetivo=1)
            mask2 = (clases == 1) | (clases == 0)
            p2, b2, e2, ph2 = entrenar_perceptron(datos[mask2], clases[mask2], epocas, tasa=tasa, clase_objetivo=1)
            mask3 = (clases == -1) | (clases == 0)
            p3, b3, e3, ph3 = entrenar_perceptron(datos[mask3], clases[mask3], epocas, tasa=tasa, clase_objetivo=-1)
            exportar_json_3_clases(
                ruta_json, datos, clases,
                e1, ph1, p1, b1, e2, ph2, p2, b2, e3, ph3, p3, b3,
                tasa_usada=tasa,
            )
        print("OK", ruta_json)
        return

    if accion == "evaluar":
        if len(sys.argv) < 6:
            print("evaluar requiere: ruta_json evaluar x1 x2 x3")
            sys.exit(1)
        x1, x2, x3 = float(sys.argv[3]), float(sys.argv[4]), float(sys.argv[5])
        clase = evaluar_desde_json(ruta_json, x1, x2, x3)
        print(clase)
        return

    # Legacy: todo en uno (generar + entrenar)
    modo = accion
    if modo == "2clases":
        if len(sys.argv) < 9:
            print("2clases requiere: ruta_json 2clases media1 desv1 media2 desv2 instancias epocas [tasa] [semilla]")
            sys.exit(1)
        media1, desv1 = float(sys.argv[3]), float(sys.argv[4])
        media2, desv2 = float(sys.argv[5]), float(sys.argv[6])
        instancias = int(sys.argv[7])
        epocas = int(sys.argv[8])
        if instancias < 4:
            print("ERROR: mínimo 4 instancias (2 por clase) para datos supervisados de entrenamiento")
            sys.exit(1)
        tasa_legacy = float(sys.argv[9]) if len(sys.argv) > 9 else 0.1
        semilla = int(sys.argv[10]) if len(sys.argv) > 10 and sys.argv[10].isdigit() else None
        if semilla is not None:
            np.random.seed(semilla)
        datos, clases = generar_datos_2_clases(media1, desv1, media2, desv2, instancias)
        pesos, bias, errores, pesos_hist = entrenar_perceptron(datos, clases, epocas, tasa=tasa_legacy, clase_objetivo=1)
        exportar_json_2_clases(ruta_json, datos, clases, errores, pesos_hist, pesos, bias, tasa_usada=tasa_legacy)
    elif modo == "3clases":
        if len(sys.argv) < 11:
            print("3clases requiere: ruta_json 3clases media1 desv1 media2 desv2 media3 desv3 instancias epocas [tasa] [semilla]")
            sys.exit(1)
        media1, desv1 = float(sys.argv[3]), float(sys.argv[4])
        media2, desv2 = float(sys.argv[5]), float(sys.argv[6])
        media3, desv3 = float(sys.argv[7]), float(sys.argv[8])
        instancias = int(sys.argv[9])
        epocas = int(sys.argv[10])
        if instancias < 6:
            print("ERROR: mínimo 6 instancias (2 por clase) para datos supervisados de entrenamiento")
            sys.exit(1)
        tasa_legacy = float(sys.argv[11]) if len(sys.argv) > 11 else 0.1
        semilla = int(sys.argv[12]) if len(sys.argv) > 12 and sys.argv[12].isdigit() else None
        if semilla is not None:
            np.random.seed(semilla)
        datos, clases = generar_datos_3_clases(media1, desv1, media2, desv2, media3, desv3, instancias)
        mask1 = (clases == 1) | (clases == -1)
        p1, b1, e1, ph1 = entrenar_perceptron(datos[mask1], clases[mask1], epocas, tasa=tasa_legacy, clase_objetivo=1)
        mask2 = (clases == 1) | (clases == 0)
        p2, b2, e2, ph2 = entrenar_perceptron(datos[mask2], clases[mask2], epocas, tasa=tasa_legacy, clase_objetivo=1)
        mask3 = (clases == -1) | (clases == 0)
        p3, b3, e3, ph3 = entrenar_perceptron(datos[mask3], clases[mask3], epocas, tasa=tasa_legacy, clase_objetivo=-1)
        exportar_json_3_clases(
            ruta_json, datos, clases,
            e1, ph1, p1, b1, e2, ph2, p2, b2, e3, ph3, p3, b3,
            tasa_usada=tasa_legacy,
        )
    else:
        print("Modo debe ser generar, entrenar, evaluar, 2clases o 3clases")
        sys.exit(1)
    print("OK", ruta_json)


if __name__ == "__main__":
    main()
