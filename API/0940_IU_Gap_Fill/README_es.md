# Estrategia IU de Relleno de Gaps
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia IU Gap Fill entra en operaciones cuando el precio forma un gap respecto al cierre de la sesión anterior y luego rellena ese gap. Se abre una posición larga después de un gap al alza que cae por debajo del cierre anterior y vuelve a cerrar por encima. Se abre una posición corta después de un gap a la baja que sube por encima del cierre anterior y vuelve a cerrar por debajo. Un stop trailing basado en ATR gestiona las salidas.

## Detalles
- **Datos**: Velas de un marco temporal definido por el usuario.
- **Criterios de entrada**:
  - **Largo**: Gap al alza de al menos `GapPercent` y el precio cruza por encima del cierre de la sesión anterior.
  - **Corto**: Gap a la baja de al menos `GapPercent` y el precio cruza por debajo del cierre de la sesión anterior.
- **Criterios de salida**: Stop trailing ATR.
- **Stops**: Nivel trailing ATR `AtrLength` * `AtrFactor`.
- **Valores predeterminados**:
  - `CandleType` = 1m
  - `GapPercent` = 0.2
  - `AtrLength` = 14
  - `AtrFactor` = 2
- **Filtros**:
  - Categoría: Gap
  - Dirección: Largo y Corto
  - Indicadores: ATR
  - Complejidad: Bajo
  - Nivel de riesgo: Medio
