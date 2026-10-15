# Estrategia de Pullback al Mínimo de las 10 Barras Solo Corto
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia entra en corto cuando el precio rompe el mínimo más bajo de las barras anteriores y la fuerza interna de la barra (IBS) está por encima de un umbral. Un filtro EMA opcional confirma la tendencia bajista.

## Detalles

- **Criterios de entrada**:
  - El mínimo rompe el mínimo más bajo de las barras anteriores de `LowestPeriod`.
  - IBS > `IbsThreshold`.
  - Opcional: precio de cierre por debajo de la EMA cuando el filtro está activado.
  - Hora dentro de `StartTime` y `EndTime`.
- **Largo/Corto**: Solo corto.
- **Criterios de salida**:
  - El precio de cierre por debajo del mínimo anterior cierra el corto.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `LowestPeriod` = 10
  - `IbsThreshold` = 0.85
  - `UseEmaFilter` = true
  - `EmaPeriod` = 200
- **Filtros**:
  - Categoría: Pullback
  - Dirección: Corto
  - Indicadores: Lowest, EMA
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
