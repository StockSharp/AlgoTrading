# Estrategia de Bot de Trading de Reversiones
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Estrategia de Bot de Trading de Reversiones utiliza la divergencia del RSI con filtros opcionales de volumen, ADX, Bandas de Bollinger y cruce de RSI para capturar reversiones del mercado. Las posiciones están protegidas con stop-loss y take-profit de porcentaje fijo.

## Detalles

- **Criterios de entrada**: divergencia del RSI con filtros opcionales de volumen, ADX, Bandas de Bollinger y cruce de RSI
- **Largo/Corto**: Ambos
- **Criterios de salida**: stop-loss o take-profit
- **Stops**: Porcentaje fijo
- **Valores predeterminados**:
  - `RsiLength` = 8
  - `FastRsiLength` = 14
  - `SlowRsiLength` = 21
  - `BbLength` = 20
  - `AdxThreshold` = 20
  - `DivLookback` = 5
  - `StopLossPercent` = 1
  - `TakeProfitPercent` = 2
- **Filtros**:
  - Categoría: Reversión
  - Dirección: Ambos
  - Indicadores: RSI, ADX, Bollinger Bands, SMA
  - Stops: Fijo
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: Sí
  - Nivel de riesgo: Medio

