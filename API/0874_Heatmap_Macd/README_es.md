# Heatmap MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este sistema utiliza un mapa de calor de histogramas MACD de cinco marcos temporales. Cuando todos los histogramas cambian por encima o por debajo de cero, entra en la dirección correspondiente y sale una vez que la alineación se rompe o se activan los límites de riesgo.

## Detalles

- **Criterios de entrada**: Todos los histogramas MACD por encima/por debajo de cero.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: La alineación de histogramas se rompe o se activan los stops.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `FastPeriod` = 20
  - `SlowPeriod` = 50
  - `SignalPeriod` = 50
  - `TakeProfitPercent` = 2
  - `StopLossPercent` = 2
  - `CandleType1` = TimeSpan.FromMinutes(60)
  - `CandleType2` = TimeSpan.FromMinutes(120)
  - `CandleType3` = TimeSpan.FromMinutes(240)
  - `CandleType4` = TimeSpan.FromMinutes(240)
  - `CandleType5` = TimeSpan.FromMinutes(480)
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Ambos
  - Indicadores: MACD
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Multi-marco temporal
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
