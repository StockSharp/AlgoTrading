# Estratégia STLMCandle
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera com base na direção da última vela concluída.
Se o preço de fechamento estiver acima do preço de abertura, abre uma posição comprada e fecha qualquer posição vendida.
Se o preço de fechamento estiver abaixo do preço de abertura, abre uma posição vendida e fecha qualquer posição comprada.
Suporta níveis de stop-loss e take-profit e opera em um período de velas configurável.

## Parâmetros
- `CandleType` – período das velas usadas para análise.
- `StopLoss` – valor absoluto de stop-loss em unidades de preço.
- `TakeProfit` – valor absoluto de take-profit em unidades de preço.

## Notas
A estratégia é uma adaptação simplificada do consultor especialista MQL original `STLMCandle`.
Ela aproxima o indicador usando os preços de abertura e fechamento padrão das velas.
