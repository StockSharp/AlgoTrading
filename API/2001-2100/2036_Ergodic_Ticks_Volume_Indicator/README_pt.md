# Estratégia de Indicador de Volume de Ticks Ergodic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia aplica o True Strength Index (TSI) aos dados de velas e o compara com uma linha de sinal de média móvel exponencial. Uma posição comprada é aberta quando o TSI cruza acima da linha de sinal, enquanto uma posição vendida é aberta quando cruza abaixo.

## Parâmetros

- **Candle Type** – período das velas usadas para os cálculos.
- **Short Length** – período de suavização rápida do TSI.
- **Long Length** – período de suavização lenta do TSI.
- **Signal Length** – período da EMA usada como linha de sinal.

## Lógica

1. Subscrever velas do período selecionado.
2. Calcular o TSI para cada vela finalizada.
3. Processar o TSI através de uma EMA para obter uma linha de sinal.
4. Quando o TSI cruza acima da linha de sinal, entrar comprado (fechando qualquer posição vendida).
5. Quando o TSI cruza abaixo da linha de sinal, entrar vendido (fechando qualquer posição comprada).

A estratégia é uma adaptação do exemplo MQL "exp_ergodic_ticks_volume_indicator.mq5" e utiliza apenas indicadores integrados do StockSharp.
