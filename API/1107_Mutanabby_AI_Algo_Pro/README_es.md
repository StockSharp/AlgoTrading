# Mutanabby AI Algo Pro
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Mutanabby AI Algo Pro entra largo cuando un patrón de vela envolvente alcista se alinea con una lectura de RSI por debajo de un umbral y una caída del precio durante un número determinado de barras. Los cierres ocurren en un patrón envolvente bajista o cuando se alcanza el stop loss.

## Detalles
- **Criterios de entrada**: Envolvente alcista, vela estable, RSI por debajo del umbral, precio por debajo del valor de N barras atrás.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: Envolvente bajista o stop loss.
- **Stops**: Opcional.
- **Valores predeterminados**:
  - `CandleStabilityIndex` = 0.5
  - `RsiIndex` = 50
  - `CandleDeltaLength` = 5
  - `DisableRepeatingSignals` = false
  - `EnableStopLoss` = true
  - `StopLossMethod` = EntryPriceBased
  - `EntryStopLossPercent` = 2.0
  - `LookbackPeriod` = 10
  - `StopLossBufferPercent` = 0.5
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Solo largos
  - Indicadores: RSI
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
