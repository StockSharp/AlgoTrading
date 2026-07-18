# Estrategia de Scalping AUD/USD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia hace scalping en AUD/USD en marcos temporales cortos usando una combinación de filtro de tendencia EMA, Bollinger Bands y RSI. Las EMAs rápida y lenta definen la dirección de la tendencia. Las operaciones largas se abren en tendencias alcistas cuando el precio toca la banda inferior de Bollinger y el RSI está por encima del umbral de sobreventa. Las posiciones cortas se toman en tendencias bajistas cuando el precio llega a la banda superior y el RSI está por debajo del nivel de sobrecompra. El take profit y stop loss fijos gestionan el riesgo.

## Detalles

- **Criterios de entrada**:
  - **Largo**: EMA rápida por encima de la EMA lenta, precio en o por debajo de la banda inferior de Bollinger, RSI por encima del nivel de sobreventa.
  - **Corto**: EMA rápida por debajo de la EMA lenta, precio en o por encima de la banda superior de Bollinger, RSI por debajo del nivel de sobrecompra.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**: Stop loss o take profit.
- **Stops**: Stop loss y take profit fijos.
- **Valores predeterminados**:
  - `EmaShort` = 13
  - `EmaLong` = 26
  - `RsiPeriod` = 4
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `BbLength` = 20
  - `BbMultiplier` = 2.0
  - `TakeProfit` = 0.0005
  - `StopLoss` = 0.0004
- **Filtros**:
  - Categoría: Scalping
  - Dirección: Ambos
  - Indicadores: EMA, Bollinger Bands, RSI
  - Stops: Fijo
  - Complejidad: Bajo
  - Marco temporal: 1 minuto
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
