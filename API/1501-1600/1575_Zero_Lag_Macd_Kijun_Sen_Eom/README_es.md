# Estrategia Zero Lag MACD + Kijun-sen + EOM
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que combina Zero Lag MACD con la línea base Kijun-sen y el filtro Ease of Movement. Usa stop y take profit basados en ATR.

## Detalles

- **Criterios de entrada**: Cruce de MACD con filtros de Kijun-sen y EOM.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Stop o take profit basados en ATR.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `MacdEmaLength` = 9
  - `KijunPeriod` = 26
  - `EomLength` = 14
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2.5m
  - `RiskReward` = 1.2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: MACD, Donchian, EOM, ATR
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
