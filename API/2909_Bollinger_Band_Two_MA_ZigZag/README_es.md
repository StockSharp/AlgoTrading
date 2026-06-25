# Estrategia Bollinger Band Two MA ZigZag
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Sistema híbrido de seguimiento de tendencia que combina reversiones por Bollinger Band, dos medias móviles de marcos temporales superiores y puntos de giro de un detector ZigZag. Abre dos posiciones en cada señal: una con un objetivo de take-profit calculado y una segunda "corriente" que depende de trailing y lógica de break-even.

## Detalles

- **Criterios de entrada**:
  - **Largo**: La barra anterior cerró por encima de la banda inferior anterior de Bollinger después de haber cerrado por debajo dos barras atrás, el cierre actual también está por encima de esa banda inferior, y el precio está por encima de ambas medias móviles de marcos temporales superiores.
  - **Corto**: La barra anterior cerró por debajo de la banda superior anterior de Bollinger después de haber cerrado por encima dos barras atrás, el cierre actual también está por debajo de esa banda superior, y el precio está por debajo de ambas medias móviles de marcos temporales superiores.
- **Gestión de posiciones**:
  - Se abren dos posiciones por señal usando `First Volume` (con take-profit) y `Second Volume` (corriente).
  - Los stops están anclados al extremo de giro ZigZag más reciente menos/más `Pivot Offset (pts)`.
  - La protección de break-even desplaza el stop a la entrada más un offset una vez que el beneficio no realizado supera `Break-even Threshold (pts)` + `Break-even Offset (pts)`.
  - El trailing stop se mueve después de que el precio avance `Trailing Step (pts)` más allá del stop existente, manteniendo una distancia de `Trailing Stop (pts)`.
- **Take Profit**:
  - El take-profit de la primera posición se calcula como un porcentaje (`Take Profit %`) de la distancia entre la entrada y el stop.
  - La posición corriente no tiene objetivo fijo y sale por stop, trailing o señales opuestas.
- **Lógica adicional**:
  - Las señales opuestas cierran inmediatamente las posiciones abiertas en la otra dirección antes de abrir nuevas operaciones.
  - El procesamiento de señales usa velas cerradas; los datos parciales son ignorados.
- **Valores predeterminados**:
  - `First Volume` = 0.1
  - `Second Volume` = 0.1
  - `Take Profit %` = 50
  - `Pivot Offset (pts)` = 10
  - `Use Break-even Move` = true
  - `Break-even Offset (pts)` = 80
  - `Break-even Threshold (pts)` = 10
  - `Trailing Stop (pts)` = 80
  - `Trailing Step (pts)` = 120
  - `Bollinger Period` = 20
  - `Bollinger Width` = 2
  - `Base Candle` = velas de 1 hora
  - `MA1 Candle` = velas diarias
  - `MA2 Candle` = velas de 4 horas
  - `MA1 Period` = 20
  - `MA2 Period` = 20
  - `ZigZag Depth` = 12
  - `ZigZag Deviation (pts)` = 5
  - `ZigZag Backstep` = 3
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Bollinger Bands, Moving Averages, ZigZag
  - Stops: Sí (stop por giro, break-even, trailing)
  - Complejidad: Avanzado
  - Marco temporal: Multi-marco temporal (base 1h, filtros Daily + 4h)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

## Notas

- La estrategia requiere suscripciones de velas en tres marcos temporales distintos para evaluar los filtros y gestionar las salidas.
- La detección de giros aproxima la lógica ZigZag de MetaTrader aplicando reglas mínimas de profundidad, desviación y backstep antes de actualizar los niveles de pivote.
- Los volúmenes se pueden ajustar de forma independiente para afinar el tamaño del tramo de take-profit frente al tramo corriente.
