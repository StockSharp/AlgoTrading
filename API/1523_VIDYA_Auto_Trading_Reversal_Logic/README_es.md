# Estrategia VIDYA de Auto-Trading (Lógica de Reversión)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia aplica una media dinámica variable VIDYA con bandas ATR amplias.
Se abre una operación larga cuando el precio rompe por encima de la banda superior, y una operación corta cuando el precio rompe por debajo de la banda inferior.

## Detalles

- **Criterios de entrada**: el precio cruza la banda ATR alrededor de VIDYA
- **Largo/Corto**: Ambos
- **Criterios de salida**: ruptura de banda opuesta
- **Stops**: No
- **Valores predeterminados**:
  - `VidyaLength` = 10
  - `VidyaMomentum` = 20
  - `BandDistance` = 2
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: VIDYA, ATR
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
