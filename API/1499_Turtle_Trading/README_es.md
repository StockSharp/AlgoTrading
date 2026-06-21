# Sistema Turtle Trading
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Sistema clásico de Turtle Trading que utiliza rupturas de canales Donchian y gestión de riesgo basada en ATR.

## Detalles

- **Criterios de entrada**: ruptura de la banda superior/inferior del canal Donchian
- **Largo/Corto**: ambos
- **Criterios de salida**: cruce de canal Donchian más corto o stop trailing
- **Stops**: stop inicial y trailing basado en ATR
- **Valores predeterminados**:
  - `EntryLength` = 20
  - `ExitLength` = 10
  - `EntryLengthMode2` = 55
  - `ExitLengthMode2` = 20
  - `AtrPeriod` = 14
  - `RiskPerTrade` = 0.02
  - `InitialStopAtrMultiple` = 2
  - `PyramidAtrMultiple` = 0.5
  - `MaxUnits` = 4
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: DonchianChannels, ATR
  - Stops: ATR
  - Complejidad: Avanzado
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
