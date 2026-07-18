# Acumulador Inteligente Ttp
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que acumula posiciones largas cuando el RSI cae por debajo de su media en una desviación estándar y las distribuye cuando el RSI sube por encima del mismo umbral.

## Detalles

- **Criterios de entrada**: RSI < SMA(RSI, `MaPeriod`) - StdDev(RSI, `StdPeriod`)
- **Largo/Corto**: Solo largos
- **Criterios de salida**: RSI > SMA(RSI, `MaPeriod`) + StdDev(RSI, `StdPeriod`) y beneficio superior a `MinProfit`
- **Stops**: No
- **Valores predeterminados**:
  - `RsiPeriod` = 7
  - `MaPeriod` = 14
  - `StdPeriod` = 14
  - `AddWhileInLossOnly` = true
  - `MinProfit` = 0m
  - `ExitPercent` = 100m
  - `UseDateFilter` = false
  - `StartDate` = 2022-06-01
  - `EndDate` = 2030-07-01
  - `CandleType` = TimeSpan.FromHours(1)
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Solo largos
  - Indicadores: RSI, MA, StdDev
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía (1h)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
