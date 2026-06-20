# Estrategia de Asimetría Estadística en Materias Primas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Asimetría Estadística en Materias Primas** clasifica los futuros de materias primas por la asimetría de su distribución de retornos. Los contratos con asimetría positiva se favorecen para posiciones largas, mientras que los de asimetría fuertemente negativa se venden en corto, asumiendo que los movimientos extremos a la baja revertirán a la media.

## Detalles
- **Criterios de entrada**: Clasificación por asimetría histórica de retornos.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Rebalanceo periódico.
- **Stops**: Sin stop explícito.
- **Valores predeterminados**:
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoría: Estadístico
  - Dirección: Ambos
  - Indicadores: Basados en precio
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
