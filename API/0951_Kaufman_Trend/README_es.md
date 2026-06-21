# Estrategia de Tendencia Kaufman
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia de Tendencia Kaufman** usa un filtro de Kalman para estimar el precio y el momentum. La fuerza de la tendencia se deriva de la componente de velocidad del filtro y se normaliza en una ventana reciente. Las entradas ocurren cuando las condiciones de tendencia fuerte se alinean con el precio por encima o por debajo del valor filtrado. Los stops se basan en oscilaciones recientes más ATR, y las ganancias se toman en etapas a medida que el momentum se debilita.

## Detalles
- **Criterios de entrada**: umbral de fuerza de tendencia con precio por encima/debajo del valor filtrado.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: toma de ganancias escalonada y debilitamiento de la tendencia o activación del stop.
- **Stops**: sí, mínimo/máximo de oscilación menos/más ATR.
- **Valores predeterminados**:
  - `TakeProfit1Percent = 50`
  - `TakeProfit2Percent = 25`
  - `TakeProfit3Percent = 25`
  - `SwingLookback = 10`
  - `AtrPeriod = 14`
  - `TrendStrengthEntry = 60`
  - `TrendStrengthExit = 40`
  - `CandleType = TimeSpan.FromMinutes(15).TimeFrame()`
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Kalman
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía (15m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
