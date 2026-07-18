# Módulo de Backtesting
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica el comportamiento predeterminado del "Backtesting Module" de TradingView. Opera un cruce de medias móviles simples: se abre una posición larga cuando la SMA de 50 períodos cruza por encima de la SMA de 200 períodos, y se abre una posición corta cuando ocurre el cruce opuesto. El trading solo está permitido entre las horas de inicio y fin especificadas.

## Detalles

- **Criterios de entrada**: SMA de 50 períodos cruzando la SMA de 200 períodos.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Cruce opuesto o salida del intervalo de tiempo.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `FastLength` = 50
  - `SlowLength` = 200
  - `StartTime` = 1 Jan 1980
  - `EndTime` = 31 Dec 2050
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: SMA
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Variable
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
