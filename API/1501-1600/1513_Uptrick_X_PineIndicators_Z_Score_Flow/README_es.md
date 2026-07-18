# Uptrick X PineIndicators: Estrategia Z-Score Flow
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de seguimiento de tendencia que utiliza Z-Score, EMA y filtros RSI.

## Detalles

- **Criterios de entrada**: Z-Score cruza los umbrales de compra/venta con confirmación de tendencia y RSI
- **Largo/Corto**: Ambos
- **Criterios de salida**: Señal opuesta basada en el modo seleccionado
- **Stops**: No
- **Valores predeterminados**:
  - `ZScorePeriod` = 100
  - `EmaTrendLen` = 50
  - `RsiLen` = 14
  - `RsiEmaLen` = 8
  - `ZBuyLevel` = -2
  - `ZSellLevel` = 2
  - `CooldownBars` = 10
  - `SlopeIndex` = 30
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: SMA, EMA, RSI, StandardDeviation
  - Stops: No
  - Complejidad: Avanzado
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
