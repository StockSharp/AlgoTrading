# Estratégia Hull Trend OSMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma conversão do consultor especialista MetaTrader "Exp_HullTrendOSMA".

## Visão geral

A estratégia usa o indicador Hull Trend OSMA, que calcula uma Hull Moving Average e uma versão suavizada dela. O valor do oscilador é a diferença entre estas duas séries. Quando o oscilador sobe por duas velas completadas consecutivas, a estratégia abre uma posição comprada. Quando o oscilador cai por duas velas completadas consecutivas, a estratégia abre uma posição vendida. Posições opostas são fechadas a cada sinal.

## Parâmetros

- **Hull Period** – período para a Hull Moving Average.
- **Signal Period** – período da média móvel de suavização aplicada ao oscilador.
- **Take Profit** – distância para ordens de take profit em unidades de preço.
- **Stop Loss** – distância para ordens de stop loss em unidades de preço.
- **Candle Type** – período das velas utilizadas para cálculos (padrão 8 horas).

## Notas

- Utiliza a API de alto nível do StockSharp com subscrição automática de velas.
- Entradas e saídas são executadas com ordens a mercado.
- A proteção de stop loss e take profit é inicializada uma vez quando a estratégia inicia.
