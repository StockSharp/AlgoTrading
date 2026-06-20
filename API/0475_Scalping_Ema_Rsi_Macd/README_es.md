# Estrategia de Scalping EMA RSI MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de scalping en velas de 30 minutos que combina cruce de EMA rápida/lenta, EMA de tendencia, filtros RSI y MACD con una condición de volumen. El stop-loss se basa en ATR y el take profit utiliza una relación riesgo-recompensa fija.

## Detalles

- **Criterios de entrada**: EMA rápida cruzando la EMA lenta en la dirección de la tendencia, RSI dentro de los límites, confirmación de MACD y volumen alto.
- **Largo/Corto**: Ambos direcciones.
- **Criterios de salida**: Stop opuesto o objetivo alcanzado.
- **Stops**: Stop-loss basado en ATR y take profit por riesgo-recompensa.
- **Valores predeterminados**:
  - `FastEmaLength` = 12
  - `SlowEmaLength` = 26
  - `TrendEmaLength` = 55
  - `RsiLength` = 14
  - `RsiOverbought` = 65
  - `RsiOversold` = 35
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `AtrLength` = 14
  - `AtrMultiplier` = 2.0
  - `RiskReward` = 2.0
  - `VolumeMaLength` = 20
  - `VolumeThreshold` = 1.3
  - `CandleType` = TimeSpan.FromMinutes(30)
- **Filtros**:
  - Categoría: Scalping
  - Dirección: Ambos
  - Indicadores: EMA, RSI, MACD, ATR, Volume
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía (30m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
