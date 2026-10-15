# Estratégia de Velas Anchored Momentum
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia converte o especialista MQL5 "AnchoredMomentumCandle" em um exemplo de StockSharp em C#. Calcula o momentum ancorado para os preços de abertura e fechamento das velas usando médias móveis exponenciais e simples. O indicador desenha uma vela sintética cuja cor reflete a direção do momentum.

Uma mudança para uma vela **azul** abre uma posição comprada e fecha qualquer vendida. Uma mudança para uma vela **rosa** abre uma posição vendida e fecha qualquer comprada.

## Parâmetros
- **Momentum Period** – comprimento das médias móveis simples.
- **Smooth Period** – comprimento das médias móveis exponenciais.
- **Candle Type** – período das velas utilizadas para os cálculos.

A estratégia subscreve as velas especificadas, calcula o indicador e emite ordens a mercado nas transições de cor.
