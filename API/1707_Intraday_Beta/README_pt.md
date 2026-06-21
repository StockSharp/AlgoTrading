# Estratégia Intraday Beta
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia busca pontos de virada intradiários usando inclinações de médias móveis suavizadas e o Índice de Força Relativa (RSI).
Uma posição comprada é aberta quando a inclinação da média móvel de 10 períodos vira para cima após um movimento descendente, o RSI está abaixo de 70
e a vela anterior é de alta. Uma posição vendida é aberta quando a inclinação vira para baixo após um movimento ascendente, o RSI está
acima de 30 e a vela anterior é de baixa.

Um filtro de Average True Range (ATR) bloqueia novas entradas quando a volatilidade está muito alta. As posições abertas são protegidas por um
stop trailing adaptativo que se move a favor da operação e sai quando o preço cruza o nível de stop.

## Parâmetros
- **RSI Period** – período do indicador RSI.
- **Trailing Stop** – distância do stop trailing em unidades de preço.
- **ATR Threshold** – valor máximo de ATR permitido para negociar.
- **Candle Type** – período das velas utilizadas para análise.
