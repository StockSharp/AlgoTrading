# Estratégia de Ativação de Trailing Stop
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia de Ativação de Trailing Stop** gerencia os níveis de stop de proteção para posições existentes. Ela não gera sinais de entrada; em vez disso, ajusta os stops após a abertura de uma posição para travar lucros.

## Parâmetros

- `TrailingStop` – distância em unidades de preço que o mercado deve se mover a favor da posição antes que um trailing stop seja ativado.
- `StopLoss` – distância inicial de stop-loss em unidades de preço (opcional). Defina como `0` para desativar.
- `CandleType` – tipo de candles usado para rastreamento de preço.

## Regras de operação

1. Quando uma posição é aberta, um stop-loss inicial é colocado se `StopLoss` for maior que zero.
2. Uma vez que o lucro excede `TrailingStop`, o nível de stop acompanha o preço mantendo a distância especificada.
3. A posição é fechada quando o preço toca o nível de trailing stop.
4. A estratégia funciona tanto para posições compradas quanto vendidas.

## Notas

Esta estratégia foi projetada para ser usada junto com outra estratégia que fornece sinais de entrada. Ela se concentra exclusivamente na gestão de saídas por meio de trailing stops.
