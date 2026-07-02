# Estrategia Larry Connors RSI 3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de reversión a la media basada en las reglas RSI de Larry Connors.

La estrategia compra cuando el precio está por encima de la SMA de 200 períodos y el RSI de 2 períodos ha caído tres días seguidos desde por encima del nivel de activación hasta territorio de sobreventa. Las posiciones se cierran cuando el RSI sube por encima del nivel de sobrecompra.

## Detalles

- **Criterios de entrada**: Cierre por encima de la SMA y RSI de 2 períodos cayendo tres días desde por encima del activador hasta la sobreventa.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: RSI por encima del nivel de sobrecompra.
- **Stops**: No.
- **Valores predeterminados**:
  - `RsiPeriod` = 2
  - `SmaPeriod` = 200
  - `DropTrigger` = 60m
  - `OversoldLevel` = 10m
  - `OverboughtLevel` = 70m
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Largo
  - Indicadores: RSI, SMA
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Diario
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
