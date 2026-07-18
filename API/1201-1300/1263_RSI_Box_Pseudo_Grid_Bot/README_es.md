# RSI Box (Bot de Pseudo Cuadrícula)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en cuadrícula que deriva niveles de precio a partir de señales de sobrecompra y sobreventa del RSI. Cuando el RSI cruza un extremo, las líneas de cuadrícula dinámicas se recalculan a partir de máximos y mínimos recientes. Las operaciones ocurren cuando el precio rompe por encima o por debajo del siguiente nivel de cuadrícula. Los cortos son opcionales.

## Detalles

- **Criterios de entrada**: El precio cruza la siguiente línea de cuadrícula después de un extremo del RSI.
- **Largo/Corto**: Largo por defecto, cortos opcionales.
- **Criterios de salida**: El precio cruza la línea de cuadrícula opuesta.
- **Stops**: No.
- **Valores predeterminados**:
  - `RsiPeriod` = 14
  - `Overbought` = 70
  - `Oversold` = 30
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `UseShorts` = false
- **Filtros**:
  - Categoría: Cuadrícula
  - Dirección: Ambos
  - Indicadores: RSI, Highest, Lowest
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
