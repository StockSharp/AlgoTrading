# Estrategia de Canal de Bandas de Regresión Polinomial
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia ajusta una línea de regresión polinomial a los precios recientes y construye bandas superior e inferior a partir de la desviación estándar de los residuos. Las posiciones largas se abren cuando el precio cae por debajo de la banda inferior y las posiciones cortas cuando el precio sube por encima de la banda superior.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `Close < LowerBand`.
  - **Corto**: `Close > UpperBand`.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Señal opuesta.
- **Stops**: No.
- **Valores predeterminados**:
  - `Length` = 100.
  - `Degree` = 2.
  - `StdDevMultiplier` = 2.
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame().
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: Regresión polinomial
  - Stops: No
  - Complejidad: Moderado
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

## Cálculo y ejecución

Cada vela terminada ajusta por mínimos cuadrados un polinomio de grado `Degree` a los últimos `Length` cierres, incluido el actual, en una cuadrícula uniforme de -1 a 1. El centro del canal es el valor ajustado de la observación más reciente. Anchura: `StdDevMultiplier * sqrt(sum((close[i] - fitted[i])^2) / Length)`, desviación poblacional de los residuos sin corrección por grados de libertad.

Un cierre estrictamente inferior a la banda baja abre largo; superior a la banda alta abre corto. La entrada opuesta invierte toda la posición con una orden de mercado de `Volume + abs(Position)`; sin posición usa `Volume`. Dentro de las bandas mantiene la posición. Solo opera con la ventana completa, sin espera ni stops protectores.

Configuración válida: `0 <= Degree <= 6`, `Length >= Degree + 2`, `StdDevMultiplier` positivo. Grado cero es una constante. La cuadrícula normalizada fija usa una base polinomial ortonormal precalculada con dos pasadas de ortogonalización; cada ventana proyecta desplazamientos de precio sobre ella en precisión double. C# y Python calculan igual; cantidades de órdenes en decimal.
