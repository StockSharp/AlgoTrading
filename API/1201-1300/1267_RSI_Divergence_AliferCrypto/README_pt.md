# Estratégia de Divergência RSI - AliferCrypto
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada em divergências de RSI com filtros opcionais de zona e tendência. O stop loss e o take profit podem ser calculados a partir de swings ou ATR com atualizações dinâmicas ou estáticas.

## Lógica
- **Entrada**
  - Divergência de alta: o preço forma uma mínima mais baixa enquanto o RSI forma uma mínima mais alta.
  - Divergência de baixa: o preço forma uma máxima mais alta enquanto o RSI forma uma máxima mais baixa.
  - O filtro opcional de zona RSI requer um estado anterior de sobrevenda/sobrecompra.
  - O filtro opcional de tendência usa a direção da média móvel.
- **Saída**
  - SL/TP a partir do swing recente ou ATR.
  - Os níveis podem ser fixados na entrada ou recalculados a cada barra.

## Indicadores
- Relative Strength Index
- Moving Average
- Average True Range
- Highest/Lowest
