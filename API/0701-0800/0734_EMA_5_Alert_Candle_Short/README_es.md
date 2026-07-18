# Estrategia Corta de Vela de Alerta EMA 5
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **EMA 5 Alert Candle Short** espera tres velas que toquen la EMA de 5 períodos y luego identifica una vela que se mantiene por encima de ella. Se abre una posición corta cuando la siguiente vela rompe el mínimo de la vela de alerta, con el take profit colocado a una distancia igual al stop loss.

## Detalles
- **Criterios de entrada**: después de tres velas que tocan la EMA, corto en la ruptura del mínimo de una vela que no toca la EMA.
- **Largo/Corto**: Solo corto.
- **Criterios de salida**: stop loss en el máximo de la vela de alerta, take profit a igual distancia.
- **Stops**: Sí, basado en el rango de la vela de alerta.
- **Valores predeterminados**:
  - `EmaPeriod = 5`
  - `RiskPerTrade = 2m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Corto
  - Indicadores: EMA
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
