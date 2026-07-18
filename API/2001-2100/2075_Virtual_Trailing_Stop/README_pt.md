# Stop Trailing Virtual
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia emula um stop trailing virtual para posições compradas e vendidas. Não gera sinais de entrada; as ordens devem ser abertas externamente ou manualmente. Uma vez que uma posição existe, a estratégia mantém um stop trailing que segue o preço à medida que ele se move em uma direção favorável. Se o preço atingir o nível do stop, a posição é fechada a mercado.

## Parâmetros

- `StopLoss` – distância fixa do stop-loss em passos de preço.
- `TakeProfit` – distância fixa do take-profit em passos de preço.
- `TrailingStop` – distância do preço atual até o stop trailing.
- `TrailingStart` – lucro mínimo em passos de preço antes do início do trailing.
- `TrailingStep` – lucro adicional mínimo necessário para mover o nível de trailing.
- `CandleType` – série de velas usada para processar os dados de preço.

## Notas

A estratégia subscreve velas do tipo especificado e avalia a lógica de trailing apenas em velas fechadas.
