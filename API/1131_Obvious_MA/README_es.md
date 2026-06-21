# Estrategia OBVious MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia abre una posición larga cuando el OBV cruza por encima de su media móvil de entrada larga y sale cuando el OBV cruza por debajo de la media de salida larga. Las posiciones cortas se abren cuando el OBV cruza por debajo de su media de entrada corta y se cierran cuando cruza por encima de la media de salida corta. Un filtro de dirección permite habilitar solo operaciones largas o cortas.

## Detalles

- **Criterios de entrada**:
  - **Largo**: OBV cruza por encima de la MA de entrada larga y la dirección no es Short.
  - **Corto**: OBV cruza por debajo de la MA de entrada corta y la dirección no es Long.
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Largo: OBV cruza por debajo de la MA de salida larga.
  - Corto: OBV cruza por encima de la MA de salida corta.
- **Stops**: No.
- **Valores predeterminados**:
  - `LongEntryLength` = 190
  - `LongExitLength` = 202
  - `ShortEntryLength` = 395
  - `ShortExitLength` = 300
  - `TradeDirection` = "Long"
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: OBV, SMA
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
