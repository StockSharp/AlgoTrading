# Estrategia de Mercados Tendenciales Nifty Options con TSL
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de Ruptura usando Bollinger Bands con filtros ADX y Supertrend. Las entradas requieren un pico de volumen. Las posiciones se cierran en cruces de MACD, debilitamiento del ADX o un trailing stop basado en ATR.

## Detalles

- **Criterios de entrada**:
  - Largo: precio cruza por encima de la banda superior de Bollinger && ADX > umbral && pico de volumen && precio por encima de Supertrend
  - Corto: precio cruza por debajo de la banda inferior de Bollinger && ADX > umbral && pico de volumen && precio por debajo de Supertrend
- **Largo/Corto**: Ambos
- **Criterios de salida**: Cruce de MACD, caída de ADX o trailing stop por ATR
- **Stops**: Trailing stop por ATR
- **Valores predeterminados**:
  - `BollingerPeriod` = 20
  - `BollingerMultiplier` = 2m
  - `AdxLength` = 14
  - `AdxEntryThreshold` = 25m
  - `AdxExitThreshold` = 20m
  - `SuperTrendLength` = 10
  - `SuperTrendMultiplier` = 3m
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `AtrLength` = 14
  - `AtrMultiplier` = 1.5m
  - `VolumeSpikeMultiplier` = 1.5m
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Bollinger Bands, ADX, Supertrend, MACD, ATR
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
