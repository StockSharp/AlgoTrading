# Estrategia Global Index Spread RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Global Index Spread RSI opera el E-mini S&P 500 cuando su diferencial respecto a un índice bursátil global se vuelve sobrevendido. El diferencial se mide en términos porcentuales y se procesa mediante un RSI de período corto. Se abre una posición larga cuando el RSI cae por debajo del umbral de sobreventa y se cierra cuando supera el umbral de sobrecompra.

## Detalles
- **Datos**: Cierres diarios de ES y del índice global.
- **Criterios de entrada**:
  - **Largo**: RSI del diferencial por debajo de `OversoldThreshold`.
- **Criterios de salida**: RSI del diferencial por encima de `OverboughtThreshold`.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `RsiLength` = 2
  - `OversoldThreshold` = 35
  - `OverboughtThreshold` = 78
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Largo
  - Indicadores: RSI
  - Complejidad: Bajo
  - Nivel de riesgo: Medio
