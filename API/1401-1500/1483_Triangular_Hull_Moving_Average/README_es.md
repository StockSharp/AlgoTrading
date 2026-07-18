# Estrategia de Media Móvil Hull Triangular
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en el cruce de la Media Móvil Hull con un desfase de dos barras.

La estrategia compara la Media Móvil Hull con su valor de hace dos barras. Un cruce al alza abre una posición larga, mientras que un cruce a la baja abre una posición corta. La dirección puede limitarse a modo solo largo o solo corto.

## Detalles
- **Criterios de entrada**: Cruce de HMA con desfase de 2 barras.
- **Largo/Corto**: Configurable.
- **Criterios de salida**: Señal opuesta o filtro de dirección.
- **Stops**: No.
- **Valores predeterminados**:
  - `Length` = 40
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `EntryMode` = EntryDirection.LongAndShort
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Configurable
  - Indicadores: MA
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
