# Estrategia Supertrend Advance de Retroceso
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Supertrend Advance Pullback combina Supertrend con entradas por retroceso o cambio de tendencia. Los filtros opcionales de EMA, RSI, MACD y CCI refinan las señales.

## Detalles

- **Criterios de entrada**: Retroceso o giro de Supertrend con filtros opcionales de EMA, RSI, MACD, CCI
- **Largo/Corto**: Ambos
- **Criterios de salida**: señal opuesta
- **Stops**: No
- **Valores predeterminados**:
  - `AtrLength` = 10
  - `Factor` = 3
  - `EmaLength` = 200
  - `UseEmaFilter` = true
  - `UseRsiFilter` = true
  - `RsiLength` = 14
  - `RsiBuyLevel` = 50
  - `RsiSellLevel` = 50
  - `UseMacdFilter` = true
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
  - `UseCciFilter` = true
  - `CciLength` = 20
  - `CciBuyLevel` = 200
  - `CciSellLevel` = -200
  - `Mode` = Pullback
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Supertrend, EMA, RSI, MACD, CCI
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
