# Estratégia Charles 1.3.7
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia coloca ordens stop simétricas acima e abaixo do preço atual e usa saídas trailing para capturar rompimentos.

## Parâmetros

- **Anchor** – distância em passos de preço para colocar as ordens stop.
- **XFactor** – multiplicador para o volume da ordem.
- **Trailing Stop** – distância do trailing stop em passos de preço.
- **Trailing Profit** – limiar de lucro para sair da posição.
- **Stop Loss** – stop loss fixo em passos de preço (0 desabilita).
- **Volume** – volume base da ordem.
- **Candle Type** – período dos candles processados.

## Lógica de Negociação

1. Quando não há posição aberta, as ordens existentes são canceladas e um Buy Stop e um Sell Stop são colocados a `Anchor` passos do fechamento do último candle.
2. Quando uma posição é aberta, a ordem stop oposta é cancelada. O preço de entrada é lembrado para cálculos de saída.
3. Para uma posição comprada, se o lucro atingir `Trailing Profit` ou o preço cair `Stop Loss`, a posição é fechada. Para uma posição vendida, a lógica é espelhada.

A estratégia é projetada como exemplo de negociação de rompimento com gerenciamento de risco simples.
