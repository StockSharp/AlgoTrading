# Estrategia de Reversión de Tendencia EMA RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que entra en largo cuando el EMA cruza con confirmación del RSI y sale cuando se produce el cruce opuesto con RSI por debajo del nivel. Usa take profit y stop loss basados en porcentaje.

## Detalles

- **Criterios de entrada**:
  - Largo: `FastEMA crosses above SlowEMA && RSI > RsiLevel`
- **Largo/Corto**: Solo largos
- **Stops**: Take profit y stop loss porcentuales
- **Valores predeterminados**:
  - `FastLength` = 9
  - `SlowLength` = 21
  - `RsiLength` = 14
  - `RsiLevel` = 50m
  - `TakeProfitPercent` = 2m
  - `StopLossPercent` = 1m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Solo largos
  - Indicadores: EMA, RSI
  - Stops: Sí
  - Complejidad: Principiante
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
