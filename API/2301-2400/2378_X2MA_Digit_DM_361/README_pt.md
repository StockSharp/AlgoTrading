# Estratégia X2MA Digit DM 361
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia combina duas médias móveis com o Índice de Direção Média (ADX).
Uma posição comprada é aberta quando a média móvel rápida está acima da lenta e o índice direcional positivo (+DI) é maior que o negativo (-DI).
Uma posição vendida é aberta quando a média móvel rápida está abaixo da lenta e -DI é maior que +DI.

A estratégia usa proteções de stop-loss e take-profit baseadas em percentagem. As velas para os cálculos são obtidas do período especificado.

## Parâmetros
- **Fast MA Length** – comprimento da média móvel rápida.
- **Slow MA Length** – comprimento da média móvel lenta.
- **ADX Length** – período para o cálculo do Índice de Direção Média.
- **Stop Loss %** – tamanho do stop-loss em percentagem do preço de entrada.
- **Take Profit %** – tamanho do take-profit em percentagem do preço de entrada.
- **Candle Type** – período das velas utilizado para o processamento.
