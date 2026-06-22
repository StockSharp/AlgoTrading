# Estrategia de Rompimiento de Canal de Regresión
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia implementa un sistema de trading basado en canal de regresión a partir del script MQL `e-Regr`.
Construye una línea de regresión lineal sobre un número configurable de velas recientes y
añade bandas superior e inferior a una distancia de desviación estándar especificada. Reglas de trading:

- **Entrada larga:** cuando el mínimo de la vela toca o rompe por debajo de la banda inferior.
- **Entrada corta:** cuando el máximo de la vela toca o rompe por encima de la banda superior.
- **Salida:** cuando el precio de cierre cruza la línea de regresión en dirección opuesta.
- **Stop Trailing:** la lógica trailing opcional mueve el nivel del stop después de que la operación
  ha alcanzado un beneficio configurado.

## Parámetros

| Nombre          | Descripción                                                     |
|-----------------|-----------------------------------------------------------------|
| `CandleType`    | Tipo de vela utilizada para los cálculos.                       |
| `Length`        | Número de velas para la regresión y la desviación estándar.     |
| `Deviation`     | Multiplicador de desviación estándar para el ancho del canal.   |
| `UseTrailing`   | Activa la lógica de stop trailing.                              |
| `TrailingStart` | Beneficio requerido antes de que comience el trailing.          |
| `TrailingStep`  | Distancia entre el precio y el stop trailing.                   |

La estrategia usa la API de alto nivel de StockSharp mediante `SubscribeCandles` y `Bind`
para recibir datos de velas y valores de indicadores.
