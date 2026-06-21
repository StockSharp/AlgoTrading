# Estrategia de Day Trading MACD RSI EMA BB ATR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia intradía que combina el cruce de señal MACD, límites de RSI y dirección de tendencia EMA con un filtro de contracción de Bandas de Bollinger. La gestión del riesgo utiliza stop-loss basado en ATR, stop de seguimiento y take-profit de riesgo-recompensa.

## Detalles

- **Criterios de entrada**: MACD cruzando la señal en la dirección de la tendencia, RSI dentro de umbrales y sin contracción de BB.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Stop opuesto u objetivo.
- **Stops**: Stop-loss basado en ATR, stop de seguimiento y take-profit de riesgo-recompensa.
- **Valores predeterminados**:
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `EmaFast` = 9
  - `EmaSlow` = 21
  - `AtrLength` = 14
  - `AtrMultiplier` = 2.0
  - `TrailAtrMultiplier` = 1.5
  - `BbLength` = 20
  - `BbMultiplier` = 2.0
  - `RiskReward` = 2.0
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: MACD, RSI, EMA, Bollinger Bands, ATR
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
