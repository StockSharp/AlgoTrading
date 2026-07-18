# Estratégia de Rompimento para Iniciantes
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Usa os preços mais altos e mais baixos das últimas `Period` velas para formar um canal. Quando o fechamento se aproxima do limite superior, a estratégia vai comprada. Quando o fechamento se aproxima do limite inferior, vai vendida.

## Regras de Entrada
- **Comprado**: Close >= highest - (highest - lowest) * `ShiftPercent` / 100 e a tendência ainda não está em alta.
- **Vendido**: Close <= lowest + (highest - lowest) * `ShiftPercent` / 100 e a tendência ainda não está em baixa.

## Regras de Saída
- O sinal oposto fecha a posição atual e abre uma nova na outra direção.

## Parâmetros
- `Period` – barras para olhar para trás no cálculo do canal.
- `ShiftPercent` – deslocamento percentual das bordas do canal.
- `CandleType` – período das velas de trabalho.
- `Volume` – volume de negociação.
- `StopLoss` – stop loss em unidades de preço.
- `TakeProfit` – take profit em unidades de preço.

## Indicadores
- Highest
- Lowest
