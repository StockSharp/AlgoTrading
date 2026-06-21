# Estrategia Nifty 50 de 5 Minutos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia Nifty 50 de 5 Minutos** opera rupturas en el índice Nifty 50 utilizando confirmación de DEMA, VWAP y Bandas de Bollinger.

## Detalles
- **Criterios de entrada**:
  - **Largo**: cierre por encima del máximo anterior, cierre por encima de la banda superior de Bollinger y DEMA por encima del VWAP.
  - **Corto**: cierre por debajo del mínimo anterior, cierre por debajo de la banda inferior de Bollinger y DEMA por debajo del VWAP.
- **Largo/Corto**: ambos.
- **Criterios de salida**: stop-loss.
- **Stops**: sí, puntos fijos.
- **Valores predeterminados**:
  - `DemaPeriod = 6`
  - `BollingerLength = 20`
  - `BollingerStdDev = 2`
  - `LookbackPeriod = 5`
  - `StopLossPoints = 25`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: DEMA, VWAP, Bollinger Bands
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
