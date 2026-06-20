# Bbsr Extreme
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Bbsr Extreme** combina rupturas de Bollinger Bands con un filtro de tendencia basado en una media móvil.
Una posición larga aparece cuando el precio rebota desde la banda inferior mientras la media está subiendo.
Una posición corta se abre en un retroceso desde la banda superior cuando la media desciende.
Las salidas se basan en stop-loss y take profit calculados mediante ATR.

## Detalles
- **Criterios de entrada**: El precio cruza las bandas con confirmación de tendencia.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Stop ATR o take profit.
- **Stops**: Sí, basados en ATR.
- **Valores predeterminados**:
  - `BollingerPeriod = 20`
  - `BollingerMultiplier = 2`
  - `MaLength = 7`
  - `AtrLength = 14`
  - `AtrStopMultiplier = 2`
  - `AtrProfitMultiplier = 3`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Bollinger Bands, EMA, ATR
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
