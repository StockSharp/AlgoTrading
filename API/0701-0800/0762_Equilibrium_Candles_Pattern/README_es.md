# Estrategia de Patrón de Velas de Equilibrio
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que utiliza velas de equilibrio para detectar tendencias cortas y entrar en los retrocesos. El equilibrio es el punto medio entre los precios más altos y más bajos durante una ventana de retrovisión. Después de una racha alcista o bajista, un movimiento de regreso a través del equilibrio desencadena una entrada. El ATR se utiliza para el stop/objetivo opcional y para salir en velas inusualmente grandes.

## Detalles

- **Criterios de entrada**:
  - **Largo**: Después de una tendencia alcista cuando el precio cruza por debajo del equilibrio.
  - **Corto**: Después de una tendencia bajista cuando el precio cruza por encima del equilibrio.
- **Largo/Corto**: Ambos
- **Stops**: Stop loss y take profit basados en ATR (opcional)
- **Valores predeterminados**:
  - `EquilibriumLength` = 9
  - `CandlesForTrend` = 7
  - `MaxPullbackCandles` = 2
  - `AtrPeriod` = 14
  - `StopMultiplier` = 2
  - `UseTpSl` = true
  - `UseBigCandleExit` = true
  - `BigCandleMultiplier` = 1
  - `UseReverse` = false
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
