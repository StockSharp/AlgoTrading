# Estratégia de Stop Loss para Ponto de Equilíbrio
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia move o stop loss protetor para o preço de entrada assim que a posição atinge um lucro especificado em pips. É útil para assegurar ganhos sem ajustar ordens manualmente.

## Como funciona

- Monitora o preço usando o tipo de candle selecionado.
- Quando o lucro da posição atual supera o número configurado de pips, uma ordem stop é colocada no preço de entrada.
- Funciona tanto para posições compradas quanto vendidas e calcula automaticamente o tamanho do pip usando o passo de preço do instrumento.

## Parâmetros

| Nome | Descrição |
| ---- | ----------- |
| **BreakEvenPips** | Lucro em pips necessário antes de mover o stop loss para o preço de entrada. |
| **CandleType** | Tipo de candles usados para monitorar os movimentos de preço. |

## Notas

A estratégia não gera sinais de entrada. As posições devem ser abertas por outras estratégias ou manualmente. Uma vez fechada a posição, o estado interno é reiniciado para aguardar a próxima operação.
