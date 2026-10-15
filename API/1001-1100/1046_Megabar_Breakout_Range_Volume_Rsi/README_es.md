# Ruptura Megabar (Range y Volumen y RSI)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La ruptura Megabar detecta velas grandes respaldadas por alto volumen y confirmación RSI. La estrategia entra largo en megabares alcistas y corto en bajistas.

Multiplica el rango y volumen promedio para encontrar megabares. La media móvil del RSI filtra las operaciones.

## Detalles

- **Criterios de entrada**: El cuerpo de la vela y el volumen superan sus medias móviles por los multiplicadores dados. RSI MA por encima del umbral largo para compras y por debajo del umbral corto para ventas.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Stop loss o take profit.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `VolumeAveragePeriod` = 20
  - `VolumeMultiplier` = 3
  - `RangeAveragePeriod` = 20
  - `RangeMultiplier` = 4
  - `RsiPeriod` = 14
  - `RsiMaPeriod` = 14
  - `LongRsiThreshold` = 50
  - `ShortRsiThreshold` = 70
  - `TakeProfit` = 400
  - `StopLoss` = 300
  - `FilterTradeHours` = false
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Volumen, Range, RSI
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
