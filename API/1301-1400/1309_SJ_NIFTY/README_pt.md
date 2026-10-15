# Estratégia SJ NIFTY
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de seguimento de tendência usando SuperTrend, VWAP, RSI e EMA200. A base do Canal Keltner atua como filtro de tendência opcional. O tamanho da posição é calculado a partir do percentual de risco do capital com stop-loss e take-profit baseado em relação risco/retorno.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Fechamento > SuperTrend && Fechamento > VWAP && RSI > Sobrecomprado && Fechamento > EMA200 && filtro base Keltner && Fechamento > máxima anterior.
  - **Vendido**: Fechamento < SuperTrend && Fechamento < VWAP && RSI < Sobrevendido && Fechamento < EMA200 && filtro base Keltner && Fechamento < mínima anterior.
- **Critérios de saída**: Stop-loss ou take-profit baseado na relação de risco.
- **Dimensionamento de posição**: Percentual de risco do portfólio dividido pela distância ao stop, arredondado para o tamanho do lote.
- **Indicadores**: SuperTrend, VWAP, RSI, EMA, Keltner Channels.
