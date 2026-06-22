# Estratégia de Retrocesso de Preço
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera gaps de preço diários.
No início de um dia da semana selecionado, compara o último preço de fechamento com o preço de abertura 24 horas antes.
Se o gap for maior que o parâmetro **Corridor**, abre uma posição na direção do retrocesso:

- Gap para cima → vender.
- Gap para baixo → comprar.

As operações utilizam stop loss e take-profit fixos em unidades de preço.
Um trailing stop com passo é aplicado após a posição avançar em lucro.
Todas as posições são fechadas perto do final do dia (22:45).

## Parâmetros
- `Corridor` – limiar do gap.
- `StopLoss` – distância de stop loss fixo.
- `TakeProfit` – alvo de take-profit fixo.
- `TrailingStop` – distância do trailing stop.
- `TrailingStep` – movimento necessário para atualizar o trailing.
- `TradingDay` – dia da semana para abrir operações (0=domingo).
- `CandleType` – período para os cálculos.
