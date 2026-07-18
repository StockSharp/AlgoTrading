# Estrategia de Cuadrícula TTM
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Estrategia de Cuadrícula TTM construye cuadrículas de compra y venta basadas en un estado TTM simple derivado de la EMA de máximos y mínimos. La cuadrícula se restablece cuando el estado cambia, y se colocan órdenes cada vez que el precio toca un nivel de la cuadrícula.

## Detalles

- **Criterios de entrada**: El precio alcanza el nivel de cuadrícula según el estado TTM.
- **Largo/Corto**: Ambos direcciones.
- **Criterios de salida**: Ninguno (las posiciones se acumulan).
- **Stops**: No.
- **Valores predeterminados**:
  - `TtmPeriod` = 6
  - `GridLevels` = 5
  - `GridSpacing` = 0.01m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Grid
  - Dirección: Ambos
  - Indicadores: EMA
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
