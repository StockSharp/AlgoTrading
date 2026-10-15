# Estratégia de ruptura de alcance
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia mede os preços mais altos e mais baixos nas últimas `RangePeriod` velas. Quando a vela fecha fora deste intervalo e a largura total do intervalo é mais estreita que `MaxRangePoints`, a estratégia entra na direção do rompimento.

## Regras de entrada
- **Longo**: fechamento da vela >= máxima mais alta da faixa de lookback E faixa em pontos <= `MaxRangePoints` E nenhuma posição aberta.
- **Short**: fechamento da vela <= mínimo mais baixo do intervalo de lookback E intervalo em pontos <= `MaxRangePoints` E nenhuma posição aberta.

## Regras de saída
- O stop loss e o take-profit protetores são aplicados imediatamente após a abertura da posição.
- Nenhuma regra de saída adicional é usada; a posição permanece aberta até que a proteção a feche.

## Parâmetros
- `RangePeriod` – número de velas para cálculo mais alto/mais baixo.
- `MaxRangePoints` – largura máxima do intervalo em pontos para permitir a negociação.
- `CandleType` – período de velas usado para análise e negociação.
- `Volume` – volume de ordens de mercado.
- `StopLossPoints` – distância de stop loss em pontos.
- `TakeProfitPoints` – distância de lucro em pontos.

## Indicadores
- Mais alto
- Mais baixo
