# Estrategia Arsi Vwap Atr
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de RSI adaptativo donde los niveles de sobrecompra y sobreventa se expanden o contraen según ATR o la desviación del VWAP. Las posiciones se abren en cruces del RSI sobre los niveles adaptativos y se cierran cuando el RSI regresa a la zona media.

## Detalles

- **Criterios de entrada**:
  - Largo: `RSI` cruza por encima de la línea adaptativa de sobreventa
  - Corto: `RSI` cruza por debajo de la línea adaptativa de sobrecompra
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - RSI cruza de nuevo por 50 o la línea adaptativa opuesta
- **Stops**: Porcentual usando `StopLossPercent` y `RiskReward`
- **Valores predeterminados**:
  - `RsiLength` = 14
  - `BaseK` = 1m
  - `RiskPercent` = 2m
  - `StopLossPercent` = 2.5m
  - `RiskReward` = 2m
  - `SourceOb` = ATR
  - `SourceOs` = ATR
  - `AtrLengthOb` = 14
  - `AtrLengthOs` = 14
  - `ObMultiplier` = 10m
  - `OsMultiplier` = 10m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Ambos
  - Indicadores: RSI, ATR, VWAP
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
