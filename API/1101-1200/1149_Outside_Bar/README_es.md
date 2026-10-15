# Estrategia de Barra Exterior
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera rompimientos de barras exteriores. Una barra exterior alcista ocurre cuando el máximo de la vela actual está por encima del máximo anterior y su mínimo está por debajo del mínimo anterior. Las órdenes se colocan dentro de la barra con toma de beneficios parcial opcional y movimiento del stop al punto de equilibrio.

## Detalles

- **Criterios de entrada**: Barra exterior con clasificación alcista o bajista.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Stop-loss o take-profit derivados del rango de la barra.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `CandleType` = 5 minute
  - `EntryPercentage` = 0.5
  - `TpPercentage` = 1
  - `PartialRR` = 1
  - `PartialExitPercent` = 0.5
  - `StopLossOffset` = 10
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Ambos
  - Indicadores: Candlestick
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
