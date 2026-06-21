# RSI Adaptativo T3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de seguimiento de tendencia basada en la media móvil T3 de Tillson adaptada al RSI. Entra en largo cuando el T3 cruza por encima de su valor de dos barras atrás y sale en el cruce opuesto.

Los backtests en gráficos diarios muestran un rendimiento estable en mercados con tendencia.

## Detalles

- **Criterios de entrada**: T3 cruza por encima de su valor de 2 barras atrás.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: Cruce opuesto.
- **Stops**: No.
- **Valores predeterminados**:
  - `RsiLength` = 14
  - `MinT3Length` = 5
  - `MaxT3Length` = 50
  - `VolumeFactor` = 0.7
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Solo largos
  - Indicadores: RSI, T3
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Diario
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
