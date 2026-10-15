# Estrategia EMA RSI con Stop Trailing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera cruces de EMA corta y media filtrados por una EMA larga. Los niveles de RSI cierran las operaciones, y un stop trailing con un stop-loss fijo gestiona el riesgo. Las operaciones pueden cerrarse opcionalmente tras un número de barras si son rentables.

## Detalles

- **Criterios de entrada**: EMA A cruzando EMA B con tendencia confirmada por EMA C y dirección de la vela.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Umbrales de RSI, stop trailing o salida basada en tiempo.
- **Stops**: Stop fijo en porcentaje que se convierte en trailing stop después de que el precio se mueve `TrailOffset`.
- **Valores predeterminados**:
  - `EmaALength` = 10
  - `EmaBLength` = 20
  - `EmaCLength` = 100
  - `RsiLength` = 14
  - `ExitLongRsi` = 70
  - `ExitShortRsi` = 30
  - `TrailPoints` = 50
  - `TrailOffset` = 10
  - `FixStopLossPercent` = 5
  - `CloseAfterXBars` = true
  - `XBars` = 24
  - `ShowLong` = true
  - `ShowShort` = false
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: EMA, RSI
  - Stops: Trailing
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
