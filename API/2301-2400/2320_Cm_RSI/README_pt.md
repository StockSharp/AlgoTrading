# Estratégia cm RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia é um port direto do especialista MetaTrader 4 "cm_RSI". Usa o indicador de Força Relativa (RSI) para capturar reversões de momentum.

O algoritmo monitora os valores do RSI calculados a partir dos preços de abertura das velas. Uma posição comprada é aberta quando o RSI sobe acima de um *nível de compra* configurável após estar abaixo. Uma posição vendida é aberta quando o RSI cai abaixo de um *nível de venda* configurável após estar acima. Cada negociação é protegida por valores fixos de take profit e stop loss expressos em pontos de preço.

## Lógica da estratégia

1. Calcular o RSI com um período definido pelo usuário usando os preços de abertura das velas.
2. Se o valor anterior do RSI estava abaixo do nível de compra e o valor atual cruza acima, abrir uma posição comprada a mercado.
3. Se o valor anterior do RSI estava acima do nível de venda e o valor atual cruza abaixo, abrir uma posição vendida a mercado.
4. Cada negociação usa o mesmo volume configurável e é protegida com ordens de stop loss e take profit.

## Parâmetros

| Nome | Descrição |
|------|-----------|
| `RsiPeriod` | Período de cálculo do RSI. |
| `BuyLevel` | Nível do RSI usado para acionar entradas compradas. |
| `SellLevel` | Nível do RSI usado para acionar entradas vendidas. |
| `TakeProfit` | Take profit em pontos de preço absolutos. |
| `StopLoss` | Stop loss em pontos de preço absolutos. |
| `OrderVolume` | Volume aplicado a cada negociação. |
| `CandleType` | Tipo de velas usadas para os cálculos. |

## Notas

- A estratégia processa apenas velas concluídas.
- Mantém uma única posição aberta a qualquer momento.
- `StartProtection` é usado para gerenciar automaticamente as ordens de stop loss e take profit.

