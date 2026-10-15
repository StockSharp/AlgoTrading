# Estrategia Iron Bot de Filtro de Tendencia Estadística
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia opera rupturas basadas en niveles de tendencia estadística calculados a partir de rangos de Fibonacci y Z-score.

## Detalles

- **Criterios de entrada**:
  - **Largo**: el precio cruza por encima de la línea de tendencia y el nivel de tendencia alto con Z-score no negativo.
  - **Corto**: el precio cruza por debajo de la línea de tendencia y el nivel de tendencia bajo con Z-score no positivo.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - Stop-loss al `SlRatio` por ciento desde la entrada.
  - Take-profit en uno de cuatro niveles (`Tp1Ratio`–`Tp4Ratio`) desde la entrada.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `ZLength` = 40.
  - `AnalysisWindow` = 44.
  - `HighTrendLimit` = 0.236.
  - `LowTrendLimit` = 0.786.
  - `EmaLength` = 200.
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Z-score, EMA, acción del precio
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
