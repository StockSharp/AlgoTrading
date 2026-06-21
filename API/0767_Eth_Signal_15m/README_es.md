# Estrategia ETH Signal 15m
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia ETH Signal 15m utiliza el indicador Supertrend para detectar cambios de dirección y el RSI para filtrar las entradas. Se abre una posición larga cuando la dirección del Supertrend disminuye y el RSI está por debajo del nivel de sobrecompra. Se abre una posición corta cuando la dirección del Supertrend aumenta y el RSI está por encima del nivel de sobreventa. Las salidas utilizan stop loss y take profit basados en ATR.

## Detalles

- **Criterios de entrada**:
  - **Largo**: La dirección del Supertrend disminuye y el RSI está por debajo de `RsiOverbought`.
  - **Corto**: La dirección del Supertrend aumenta y el RSI está por encima de `RsiOversold`.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**: Stop loss y take profit basados en ATR.
- **Stops**: Stop loss de 4×ATR, take profit de 2×ATR para largo, take profit de 2.237×ATR para corto.
- **Valores predeterminados**:
  - `AtrPeriod` = 12
  - `Factor` = 2.76
  - `RsiLength` = 12
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Supertrend, RSI, ATR
  - Stops: Stop loss y take profit ATR
  - Complejidad: Bajo
  - Marco temporal: 15m
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
