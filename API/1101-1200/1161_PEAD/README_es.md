# Estrategia PEAD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera el drift posterior al anuncio de ganancias tras una sorpresa positiva de EPS y un gap alcista.
Entra en largo al día siguiente de las ganancias cuando el precio abre con un gap al alza y el rendimiento reciente es positivo,
utilizando un trailing EMA, stop fijo/breakeven y un período máximo de mantenimiento.

## Detalles

- **Criterios de entrada**: Sorpresa positiva de EPS, gap alcista tras las ganancias y rendimiento previo positivo.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: Cruce por debajo de EMA diaria, stop fijo/breakeven o máximo de barras de mantenimiento.
- **Stops**: Stop fijo con breakeven.
- **Valores predeterminados**:
  - `GapThreshold` = 1
  - `EpsSurpriseThreshold` = 5
  - `PerfDays` = 20
  - `StopPct` = 8
  - `EmaLen` = 50
  - `MaxHoldBars` = 50
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoría: Earnings
  - Dirección: Long
  - Indicadores: EMA
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Diario
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
