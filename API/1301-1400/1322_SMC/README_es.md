# Estrategia SMC
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia SMC define zonas premium, equilibrio y descuento a partir de los recientes máximos y mínimos de oscilación. Opera en zonas de descuento o premium con un filtro de tendencia SMA y confirmación simple de bloque de órdenes.

## Detalles

- **Criterios de entrada**: precio en zona de descuento por encima de la SMA con soporte de bloque de órdenes; precio en zona premium por debajo de la SMA con resistencia de bloque de órdenes
- **Largo/Corto**: Ambos
- **Criterios de salida**: señal opuesta
- **Stops**: No
- **Valores predeterminados**:
  - `SwingHighLength` = 8
  - `SwingLowLength` = 8
  - `SmaLength` = 50
  - `OrderBlockLength` = 20
- **Filtros**:
  - Categoría: Zone
  - Dirección: Ambos
  - Indicadores: Highest, Lowest, SMA
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
