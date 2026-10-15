# Estratégia Liquidex V1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Liquidex V1 é uma estratégia de scalping por rompimento convertida do assessor especialista MQL original. Combina um **filtro de intervalo** e uma **média móvel ponderada (WMA)** para identificar oportunidades de curto prazo.

## Lógica de trading
1. Para cada vela concluída, a estratégia mede seu intervalo (`high - low`).
2. Se o intervalo da vela for menor que `RangeFilter`, a vela é ignorada.
3. Uma WMA com período `MaPeriod` é calculada usando preços de fechamento.
4. Quando a vela abre abaixo da WMA e fecha acima, uma ordem de **compra** a mercado é enviada.
5. Quando a vela abre acima da WMA e fecha abaixo, uma ordem de **venda** a mercado é enviada.
6. Cada posição é protegida por um stop-loss definido em `StopLoss`.

## Parâmetros
- `RangeFilter` – intervalo mínimo da vela em unidades de preço necessário para operar.
- `MaPeriod` – número de períodos para a média móvel ponderada.
- `StopLoss` – stop-loss de proteção em pontos.
- `CandleType` – série de velas utilizada para análise.

A estratégia usa `Strategy.Volume` como tamanho da ordem e inverte a posição quando um sinal oposto aparece.
