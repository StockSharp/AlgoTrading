# Estrategia Color Zerolag Momentum X2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de momentum en doble marco temporal que utiliza un cruce de media móvil de cero lag. El marco temporal superior define la dirección de la tendencia, mientras que el marco temporal inferior activa las entradas cuando el momentum cruza su media de cero lag en la dirección de la tendencia.

## Detalles

- **Criterios de entrada**: el momentum cruza su media de cero lag en la dirección de la tendencia
- **Largo/Corto**: Ambos
- **Criterios de salida**: cruce opuesto o reversión de tendencia
- **Stops**: No
- **Valores predeterminados**:
  - `TrendCandleType` = 6h
  - `TrendMomentumPeriod` = 34
  - `TrendMaLength` = 15
  - `SignalCandleType` = 30m
  - `SignalMomentumPeriod` = 34
  - `SignalMaLength` = 15
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Momentum, ZeroLagEMA
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Multi-marco temporal
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
