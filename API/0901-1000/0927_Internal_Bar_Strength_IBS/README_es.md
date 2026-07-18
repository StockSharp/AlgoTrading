# Estrategia IBS de Fuerza Interna de Barra
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia entra en largo cuando la fuerza interna de barra (IBS) está por debajo de un umbral inferior y sale cuando el IBS sube por encima de un umbral superior dentro de una ventana de tiempo especificada.

## Detalles

- **Criterios de entrada**:
  - IBS < `LowerThreshold`.
  - Tiempo entre `StartTime` y `EndTime`.
- **Largo/Corto**: Solo largo.
- **Criterios de salida**:
  - IBS >= `UpperThreshold`.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `UpperThreshold` = 0.8
  - `LowerThreshold` = 0.2
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Solo largos
  - Indicadores: Ninguno
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
