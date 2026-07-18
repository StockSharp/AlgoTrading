# Estrategia RGT RSI Bollinger
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia combina el Índice de Fuerza Relativa (RSI) con las Bandas de Bollinger para detectar oportunidades de reversión a la media. Se abre una posición larga cuando el RSI indica un mercado sobrevendido y el precio opera por debajo de la banda inferior de Bollinger. Se entra en corto cuando el RSI muestra un mercado sobrecomprado y el precio sube por encima de la banda superior. La estrategia aplica un stop-loss inicial y luego trailinea el stop una vez que se alcanza un beneficio mínimo.

El trailing stop asegura las ganancias siguiendo al precio a una distancia fija una vez que el trade se mueve favorablemente. Las posiciones se cierran cuando se activa el trailing stop.

## Detalles

- **Criterios de entrada**: RSI por debajo de `RsiLow` y precio bajo la banda inferior para largos; RSI por encima de `RsiHigh` y precio sobre la banda superior para cortos.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Activación del trailing stop.
- **Stops**: Stop-loss inicial y trailing stop.
- **Valores predeterminados**:
  - `RsiPeriod` = 8
  - `RsiHigh` = 90
  - `RsiLow` = 10
  - `StopLossPips` = 70
  - `TrailingStopPips` = 35
  - `MinProfitPips` = 30
  - `Volume` = 1
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: RSI, Bandas de Bollinger
  - Stops: Sí
  - Complejidad: Principiante
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
