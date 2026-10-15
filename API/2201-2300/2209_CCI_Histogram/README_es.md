# Estrategia de Histograma CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza el Índice de Canal de Materias Primas (CCI) para detectar reversiones cuando el indicador abandona zonas extremas. Se abre una posición larga cuando el CCI cae por debajo del nivel superior después de haber estado por encima. Se abre una posición corta cuando el CCI sube por encima del nivel inferior después de haber estado por debajo. Niveles opcionales de stop loss y take profit en puntos pueden proteger las posiciones abiertas.

## Detalles

- **Criterios de entrada**:
  - **Largo**: CCI anterior > `UpperLevel` y CCI actual ≤ `UpperLevel`.
  - **Corto**: CCI anterior < `LowerLevel` y CCI actual ≥ `LowerLevel`.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: La señal opuesta cierra la posición existente y abre una nueva.
- **Stops**: Stop loss y take profit fijos opcionales en puntos.
- **Valores predeterminados**:
  - `CCI Period` = 14
  - `Upper Level` = 100
  - `Lower Level` = -100
  - `Stop Loss` = 100 puntos
  - `Take Profit` = 200 puntos
- **Filtros**:
  - Categoría: Reversión
  - Dirección: Ambos
  - Indicadores: CCI
  - Stops: Opcional
  - Complejidad: Simple
  - Marco temporal: Cualquiera (por defecto 4H)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

