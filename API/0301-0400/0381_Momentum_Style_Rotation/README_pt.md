# Estratégia de Rotação de Estilos por Momentum
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia em Python rotaciona entre um conjunto de ETFs de fatores e um ETF de mercado amplo. No final de cada mês, os ETFs são classificados pelo retorno total dos últimos três meses. A carteira então investe inteiramente no fundo de maior classificação para o mês seguinte, a fim de capturar o momentum de médio prazo.

A abordagem sempre mantém um único ETF e o reavalia mensalmente. Velas diárias são usadas para os cálculos e todas as negociações de rebalanceamento são executadas ao preço de mercado.

## Detalhes

- **Universo**: lista de ETFs de fatores e um ETF de referência de mercado.
- **Sinal**: calcular o retorno total de 63 dias (três meses) e selecionar o instrumento mais forte.
- **Rebalanceamento**: primeiro dia de negociação de cada mês.
- **Posicionamento**: totalmente comprado no ETF selecionado, todos os outros sem posição.
- **Controle de risco**: ordens são ignoradas quando o valor de negociação necessário cai abaixo de `MinTradeUsd`.
