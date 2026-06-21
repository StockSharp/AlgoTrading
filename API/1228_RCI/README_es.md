# Estrategia RCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Utiliza el Índice de Correlación de Rangos y su media móvil para operar cruces. Se abre una posición larga cuando el RCI sube por encima de su media móvil. Se abre una posición corta cuando cae por debajo. La dirección de la operación puede restringirse a solo largo o solo corto.

## Detalles
- **Criterios de entrada**: RCI cruzando su media móvil.
- **Largo/Corto**: Configurable (ambos, solo largo, solo corto).
- **Criterios de salida**: Cruce opuesto.
- **Stops**: No.
- **Valores predeterminados**:
  - `RciLength` = 10
  - `MaType` = SMA
  - `MaLength` = 14
  - `Direction` = Long & Short
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Configurable
  - Indicadores: RCI, MA
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
