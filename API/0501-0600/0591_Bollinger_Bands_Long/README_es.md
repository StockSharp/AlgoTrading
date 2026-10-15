# Estrategia de Bollinger Bands en Largo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia compra cuando el precio cierra por debajo de la banda inferior de Bollinger y el RSI está sobrevendido. Sale de la posición larga una vez que el precio regresa a la banda media.

## Detalles

- **Criterios de entrada**:
  - El precio cierra por debajo de la banda inferior de Bollinger.
  - RSI por debajo del nivel de sobreventa.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**:
  - El precio cierra en o por encima de la banda media de Bollinger.
- **Stops**: No.
- **Valores predeterminados**:
  - `BbLength` = 10
  - `BbDeviation` = 2
  - `RsiLength` = 14
  - `RsiOversold` = 30
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Largo
  - Indicadores: Bollinger Bands, RSI
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
