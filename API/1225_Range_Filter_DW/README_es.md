# Estrategia de Filtro de Rango DW
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia implementa un filtro de rango basado en ATR similar al Range Filter de Donovan Wall. El filtro ignora los movimientos de precio menores moviéndose solo cuando el precio supera un rango basado en volatilidad. Se abre una posición larga cuando el cierre está por encima de la banda superior, mientras que se abre una posición corta cuando el cierre está por debajo de la banda inferior.

## Detalles

- **Criterios de entrada**:
  - **Largo**: Cierre por encima de la banda superior.
  - **Corto**: Cierre por debajo de la banda inferior.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - Ruptura de la banda opuesta.
- **Stops**: No.
- **Valores predeterminados**:
  - `RangePeriod` = 14
  - `RangeMultiplier` = 2.618
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: ATR
  - Stops: No
  - Complejidad: Moderado
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
