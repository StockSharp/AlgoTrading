# Estrategia de Momentum Warrior Trading
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de momentum inspirada en Warrior Trading que combina detección de gaps, VWAP y configuraciones de rojo a verde.

## Detalles

- **Criterios de entrada**: Gap-and-go, red-to-green o rebote en VWAP con pico de volumen.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: Stop basado en ATR, objetivo de beneficio y trailing.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `GapThreshold` = 2m
  - `GapVolumeMultiplier` = 2m
  - `VwapDistance` = 0.5m
  - `MinRedCandles` = 3
  - `RiskRewardRatio` = 2m
  - `TrailingStopTrigger` = 1m
  - `MaxDailyTrades` = 2
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Largo
  - Indicadores: VWAP, RSI, EMA, ATR, Volume
  - Stops: Sí
  - Complejidad: Avanzado
  - Marco temporal: Intradía (1m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Alto
