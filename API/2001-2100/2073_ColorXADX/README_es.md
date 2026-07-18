# Estrategia ColorXADX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en el cruce de las líneas +DI y -DI confirmado por la fuerza del ADX.

El sistema monitorea los indicadores de Movimiento Direccional. Cuando +DI cruza por encima de -DI con el Índice de Movimiento Direccional Promedio superando un umbral establecido, entra en una posición larga y sale de cualquier corta existente. Por el contrario, un cruce bajista (-DI por encima de +DI) con ADX fuerte abre una posición corta y cierra las largas. Se aplican niveles de stop-loss y take-profit para gestionar el riesgo.

## Detalles

- **Criterios de entrada**: Cruce +DI/-DI con ADX por encima del umbral.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Señal opuesta o niveles de stop.
- **Stops**: Sí, stop-loss y take-profit fijos.
- **Valores predeterminados**:
  - `AdxPeriod` = 14
  - `AdxThreshold` = 30m
  - `StopLoss` = 1000
  - `TakeProfit` = 2000
  - `CandleType` = TimeSpan.FromHours(4)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: ADX, DMI
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Swing (4h)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
