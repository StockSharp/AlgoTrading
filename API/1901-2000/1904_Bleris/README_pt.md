# Estratégia Bleris
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Bleris analisa a tendência dos extremos de preço recentes para abrir operações na direção da tendência predominante.
A série de preços é dividida em três segmentos de comprimento `SignalBarSample` e as máximas mais altas e mínimas mais baixas desses segmentos são comparadas.

- **Indicadores**: Highest, Lowest
- **Parâmetros**:
  - `SignalBarSample` – número de candles por segmento.
  - `CounterTrend` – inverter a direção de negociação.
  - `Lots` – volume da ordem.
  - `CandleType` – período dos candles.
  - `AnotherOrderPips` – distância mínima em pips antes de abrir outra ordem do mesmo tipo.

## Como funciona
1. Os indicadores Highest e Lowest calculam preços extremos nas últimas `SignalBarSample` velas.
2. Máximas decrescentes sinalizam uma tendência de baixa; mínimas crescentes sinalizam uma tendência de alta.
3. A estratégia compra em tendência de alta e vende em tendência de baixa. Com `CounterTrend` ativado a lógica é invertida.
4. Novas ordens na mesma direção são ignoradas se o preço da última ordem estiver dentro de `AnotherOrderPips`.

Este exemplo usa a API de alto nível do StockSharp e destina-se a fins educativos.
