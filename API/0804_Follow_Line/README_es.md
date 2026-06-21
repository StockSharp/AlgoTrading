# Estrategia de Línea de Seguimiento
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia rastrea una línea de seguimiento derivada de rupturas de las Bandas de Bollinger con un desplazamiento ATR opcional. Las entradas ocurren cuando la línea cambia de dirección, opcionalmente confirmado por la tendencia de un marco temporal superior.

## Detalles

- **Criterios de entrada**: La línea de seguimiento cambia de dirección después de que el precio rompe las Bandas de Bollinger con confirmación opcional del marco temporal superior.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: La línea de seguimiento o la tendencia del marco temporal superior se revierte.
- **Stops**: No.
- **Valores predeterminados**:
  - `AtrPeriod` = 5
  - `BbPeriod` = 21
  - `BbDeviation` = 1
  - `UseAtrFilter` = true
  - `UseTimeFilter` = false
  - `Session` = "0000-2400"
  - `UseHtfConfirmation` = false
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `HtfCandleType` = TimeSpan.FromHours(4)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Bollinger Bands, ATR
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
