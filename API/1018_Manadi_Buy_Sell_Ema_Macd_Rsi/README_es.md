# Estrategia Manadi de Compra/Venta con EMA MACD RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de cruce de EMA con confirmaciones de MACD y RSI. Entradas al mercado con stop-loss y take-profit fijos en porcentaje.

## Detalles

- **Criterios de entrada**: Cruce de EMA con acuerdo del MACD y límites de RSI.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Stop-loss o take-profit basados en porcentaje.
- **Stops**: Basados en porcentaje.
- **Valores predeterminados**:
  - `FastEmaLength` = 9
  - `SlowEmaLength` = 21
  - `RsiLength` = 14
  - `RsiUpperLong` = 70
  - `RsiLowerLong` = 40
  - `RsiUpperShort` = 60
  - `RsiLowerShort` = 30
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `TakeProfitPercent` = 0.03
  - `StopLossPercent` = 0.015
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: EMA, MACD, RSI
  - Stops: Sí
  - Complejidad: Principiante
  - Marco temporal: Intradía (1m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
