# Estratégia de Gatilho ColorMaRsi
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é um port StockSharp do especialista MQL5 original `exp_colormarsi-trigger.mq5`. Ela compara EMAs rápidas e lentas e valores de RSI rápido e lento. O sinal combinado assume os valores -1, 0 ou +1. Uma posição é aberta quando o sinal anterior tem sinal oposto ao atual.

## Como funciona

- Quando o sinal muda de positivo para zero ou negativo, uma posição comprada é aberta e qualquer posição vendida é fechada.
- Quando o sinal muda de negativo para zero ou positivo, uma posição vendida é aberta e qualquer posição comprada é fechada.

## Parâmetros

- **Fast EMA** – período para a média móvel exponencial rápida.
- **Slow EMA** – período para a média móvel exponencial lenta.
- **Fast RSI** – período para o RSI rápido.
- **Slow RSI** – período para o RSI lento.
- **Candle Type** – período das velas usadas para o cálculo.

## Indicadores

- Média Móvel Exponencial (rápida e lenta)
- Índice de Força Relativa (rápido e lento)

Apenas velas finalizadas são processadas. As ordens são colocadas usando `BuyMarket` e `SellMarket`.
