# Cronex CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en el cruce del Índice de Canal de Materias Primas Cronex. El indicador suaviza el CCI a través de dos medias móviles exponenciales para crear una línea rápida y una lenta.

La estrategia abre una posición larga cuando la línea rápida cruza por debajo de la línea lenta y cierra cualquier posición corta. Se abre una posición corta cuando la línea rápida cruza por encima de la línea lenta y cierra cualquier posición larga.

Este enfoque contrario intenta capturar reversiones tras los cambios de momentum. Funciona en marcos temporales más altos, como velas de 4 horas.

## Detalles

- **Criterios de entrada**: Cruces de las líneas CCI rápida y lenta suavizadas.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Cruce opuesto.
- **Stops**: No.
- **Valores predeterminados**:
  - `CciPeriod` = 25
  - `FastPeriod` = 14
  - `SlowPeriod` = 25
  - `CandleType` = TimeSpan.FromHours(4)
  - `EnableLongEntry` = true
  - `EnableShortEntry` = true
  - `EnableLongExit` = true
  - `EnableShortExit` = true
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: CCI, EMA
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Swing (4h)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
