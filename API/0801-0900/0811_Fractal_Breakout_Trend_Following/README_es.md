# Seguimiento de Tendencia por Ruptura Fractal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El seguimiento de tendencia por ruptura fractal entra con una orden de compra stop por encima de un fractal alcista activado cuando la volatilidad es baja.

## Detalles

- **Criterios de entrada**: Fractal alcista por encima de los dientes del Alligator y percentil promedio del ATR por debajo del umbral; orden de compra stop al nivel del fractal.
- **Largo/Corto**: Solo largo.
- **Criterios de salida**: Stop-loss en el mayor entre el stop porcentual o la activación del fractal bajista.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `StopLossPercent` = 0.03
  - `AtrThreshold` = 50
  - `AtrPeriod` = 5
  - `CandleType` = TimeSpan.FromHours(1)
  - `TradeStart` = 2023-01-01
  - `TradeStop` = 2025-01-01
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Solo largos
  - Indicadores: Fractal, SMMA, ATR
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
