# Estrategia de Velas Zigzag
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia simple que reacciona a los puntos pivote de ZigZag. Se abre una posición larga cuando se forma un nuevo pivote mínimo, mientras que se toma una posición corta en los nuevos pivotes máximos.

## Detalles
- **Criterios de entrada**: Pivotes máximos y mínimos del ZigZag.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Pivote opuesto.
- **Stops**: No.
- **Valores predeterminados**:
  - `ZigzagLength` = 5
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Highest, Lowest
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
