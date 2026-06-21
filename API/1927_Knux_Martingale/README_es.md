# Estrategia Knux Martingale
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de martingala que aumenta el volumen de operación después de una posición perdedora. El método filtra las entradas por el Average Directional Index (ADX) para operar solo en mercados con tendencia. Las velas alcistas abren posiciones largas, las velas bajistas abren posiciones cortas.

## Detalles

- **Criterios de entrada**:
  - ADX > 25
  - Largo: `Close > Open`
  - Corto: `Close < Open`
- **Largo/Corto**: Ambos
- **Criterios de salida**: Stop loss o take profit
- **Stops**: Sí
- **Valores predeterminados**:
  - `AdxPeriod` = 14
  - `LotsMultiplier` = 1.5m
  - `StopLoss` = 150m
  - `TakeProfit` = 50m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Seguimiento de tendencia, Martingale
  - Dirección: Ambos
  - Indicadores: AverageDirectionalIndex
  - Stops: Absoluto
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Alto
