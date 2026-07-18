# Estrategia de Envolvente con Tendencia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia combina un filtro SuperTrend con patrones envolventes alcistas y bajistas. Se abre una operación cuando una vela engulle la barra anterior en la dirección de la tendencia predominante. Los niveles de stop y objetivo se calculan a partir del rango del patrón.

## Detalles

- **Criterios de entrada**: Patrón envolvente alineado con la dirección del SuperTrend.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Stop-loss o take-profit.
- **Stops**: Sí, basados en los extremos de la vela y el desplazamiento ATR.
- **Valores predeterminados**:
  - `CandleType` = 5 minutos
  - `AtrPeriod` = 10
  - `AtrMultiplier` = 3
  - `BoringThreshold` = 25
  - `EngulfingThreshold` = 50
  - `StopLevel` = 200
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Ambos
  - Indicadores: SuperTrend, Candlestick
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
