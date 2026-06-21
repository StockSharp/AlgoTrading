# MACD Multitemporal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El MACD Multitemporal combina señales MACD del marco temporal de trabajo y un marco temporal superior. Las entradas ocurren cuando ambos marcos temporales coinciden usando cruces de líneas o cruces de la línea cero.

## Detalles
- **Datos**: Velas de precio de dos marcos temporales.
- **Criterios de entrada**:
  - **Largo**: Depende del parámetro `Entry`. Por defecto, cruce alcista en ambos marcos temporales.
  - **Corto**: Opuesto al largo.
- **Criterios de salida**: Señal opuesta o stop trailing.
- **Stops**: Stop trailing opcional.
- **Valores predeterminados**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `CandleType` = tf(5)
  - `HigherCandleType` = tf(1d)
  - `ShowCurrentTimeframe` = true
  - `ShowHigherTimeframe` = true
  - `Entry` = Crossover
  - `UseTrailingStop` = false
  - `TrailingStopPercent` = 2
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Largo y Corto
  - Indicadores: MACD
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Multitemporal (5m/1d)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
