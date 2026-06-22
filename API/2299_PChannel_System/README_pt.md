# Estratégia Sistema PChannel
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O **Sistema PChannel** utiliza um rompimento de canal de preços com confirmação atrasada. Ele rastreia a máxima mais alta e a mínima mais baixa ao longo de um período configurável. Quando o preço rompe o canal e depois fecha de volta dentro, a estratégia entra na direção do rompimento enquanto fecha quaisquer posições opostas. Níveis opcionais de stop-loss e take-profit gerenciam o risco.

## Parâmetros
- `Period` – comprimento de historial para o canal.
- `Shift` – número de barras para atrasar os valores do canal.
- `StopLoss` – distância de preço absoluta para o stop de proteção.
- `TakeProfit` – distância de preço absoluta para o alvo de lucro.
- `CandleType` – série de candles utilizada para os cálculos.

## Lógica de Negociação
1. Calcular os limites do canal a partir dos últimos `Period` candles com um `Shift` opcional.
2. Se o candle anterior fechou fora do canal e o candle atual retorna dentro, abrir uma posição na direção do rompimento.
3. Fechar a posição oposta, se houver, antes de abrir uma nova.
4. Monitorar as negociações ativas e sair quando `StopLoss` ou `TakeProfit` for atingido.

Esta estratégia ainda não tem implementação em Python.
