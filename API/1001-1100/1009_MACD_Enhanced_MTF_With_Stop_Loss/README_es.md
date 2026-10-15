# Estrategia MACD Mejorada MTF con Stop-Loss
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia multi-marco temporal que utiliza puntuación basada en MACD y una línea de stop de seguimiento derivada del ATR.

## Detalles

- **Criterios de entrada**: La puntuación MACD se vuelve positiva o negativa.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Señal opuesta o ruptura de la línea de stop de seguimiento.
- **Stops**: Stop de seguimiento ATR.
- **Valores predeterminados**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `CrossScore` = 10
  - `IndicatorScore` = 8
  - `HistogramScore` = 2
  - `StopLossFactor` = 1.2
  - `StopLossPeriod` = 10
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: MACD, ATR
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
