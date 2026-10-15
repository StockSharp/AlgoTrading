# Zig Dan Zag Inversión a Largo Plazo Definitiva
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de inversión a largo plazo que combina pivotes ZigZag con un filtro de tendencia SMA lento. Se abre una posición cuando se forma un nuevo mínimo ZigZag por encima de la SMA, mientras que las salidas ocurren en pivotes opuestos por debajo de la SMA.

## Detalles
- **Criterios de entrada**: Nuevo mínimo ZigZag por encima de la SMA.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: Máximo ZigZag por debajo de la SMA.
- **Stops**: No.
- **Valores predeterminados**:
  - `ZigzagDepth` = 12
  - `SmaLength` = 200
  - `CandleType` = TimeSpan.FromHours(1)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Solo largos
  - Indicadores: Highest, Lowest, SimpleMovingAverage
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Largo plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
