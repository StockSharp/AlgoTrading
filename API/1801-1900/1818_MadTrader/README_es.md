# Estrategia Mad Trader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Mad Trader es una estrategia de seguimiento de tendencia convertida del experto MQL original "madtrader-8.7". Combina los indicadores ATR y RSI para identificar retrocesos de baja volatilidad durante una tendencia emergente. El sistema espera a que el ATR esté por debajo de un umbral especificado pero aún en ascenso y a que el RSI aumente dentro de una tendencia general alcista o bajista. Cuando estas condiciones se alinean y el cuerpo de la vela está dentro de los límites definidos, la estrategia abre una orden a mercado en la dirección sugerida por el RSI. Las posiciones están protegidas por un stop de seguimiento y un mecanismo de beneficio de cesta que cierra todas las operaciones una vez que el capital de la cuenta alcanza el crecimiento objetivo.

## Detalles

- **Criterios de entrada**:
  - El ATR está por debajo de `MaxAtr` y es mayor que el valor ATR anterior.
  - El tamaño del cuerpo de la vela está entre `MinCandle` y `MaxCandle`.
  - El horario de trading está dentro de `[StartHour, EndHour)`.
  - Tendencia RSI por encima de 50 y RSI actual subiendo pero por debajo de `RsiLowerLevel` → compra.
  - Tendencia RSI por debajo de 50 y RSI actual bajando pero por encima de `RsiUpperLevel` → venta.
  - Aplica un retraso mínimo de `TradeInterval` entre operaciones.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Stop de seguimiento alcanzado.
  - Objetivo de beneficio de cesta alcanzado (`BasketProfit` o `BasketProfit * BasketBoost`).
- **Stops**: Stop de seguimiento medido en puntos de precio.
- **Valores predeterminados**:
  - `AtrPeriod` = 14
  - `RsiPeriod` = 14
  - `TrendBars` = 60
  - `MinCandle` = 5
  - `MaxCandle` = 10
  - `MaxAtr` = 10
  - `RsiUpperLevel` = 50
  - `RsiLowerLevel` = 50
  - `StartHour` = 0
  - `EndHour` = 23
  - `TradeInterval` = 30 minutos
  - `TrailingStop` = 7
  - `BasketProfit` = 1.05
  - `BasketBoost` = 1.1
  - `RefreshHours` = 24
  - `ExponentialGrowth` = 0.01
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: ATR, RSI
  - Stops: Trailing
  - Complejidad: Moderado
  - Marco temporal: Corto plazo (velas de 5 minutos)
  - Nivel de riesgo: Medio
