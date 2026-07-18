# Estrategia Efficient Work
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza medias móviles en horizontes corto, medio y largo. Se abre una posición larga cuando la media rápida está por encima de ambas medias más lentas, y una posición corta cuando está por debajo de ellas.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `fast MA > medium MA` y `fast MA > high MA`.
  - **Corto**: `fast MA < medium MA` y `fast MA < high MA`.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Una señal contraria desencadena un reversal.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `MA Period` = 20
  - `Medium TF Multiplier` = 5
  - `High TF Multiplier` = 10
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Múltiples
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
