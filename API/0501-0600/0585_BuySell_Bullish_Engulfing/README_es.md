# Estrategia de Compra y Venta con Patrón Envolvente Alcista
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia entra en largo cuando una vela alcista envuelve completamente la barra bajista anterior y se cumplen condiciones de tendencia opcionales. El tamaño de la posición es un porcentaje del capital actual, mientras que el take profit y el stop loss cierran las operaciones automáticamente.

## Detalles

- **Criterios de entrada**: Patrón envolvente alcista con filtro de tendencia SMA opcional.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: Take profit o stop loss.
- **Stops**: Sí, tanto take profit como stop loss.
- **Valores predeterminados**:
  - `CandleType` = 15 minute
  - `TakeProfitPercent` = 2
  - `StopLossPercent` = 2
  - `OrderPercent` = 30
  - `TrendMode` = SMA50
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Solo largos
  - Indicadores: Candlestick, SMA
  - Stops: Sí
  - Complejidad: Bajo
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
