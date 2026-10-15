# Estratégia Color Zerolag RSI OSMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia utiliza um oscilador composto construído a partir de cinco cálculos RSI com diferentes períodos. A soma ponderada dos valores RSI é suavizada duas vezes para produzir uma linha OSMA de zero defasagem.

## Como Funciona

1. Calcular cinco valores RSI com períodos 8, 21, 34, 55 e 89.
2. Multiplicar cada RSI pelo seu peso e somar os resultados.
3. Aplicar dois passos de suavização à soma para obter o valor OSMA.
4. Se o OSMA virar para cima (o valor anterior era mais baixo do que dois períodos atrás e o valor atual supera o anterior), a estratégia fecha posições vendidas e opcionalmente abre uma comprada.
5. Se o OSMA virar para baixo (o valor anterior era mais alto do que dois períodos atrás e o valor atual cai abaixo do anterior), a estratégia fecha posições compradas e opcionalmente abre uma vendida.

## Parâmetros

- **Smoothing 1, Smoothing 2** – comprimentos das fases de suavização.
- **Factor 1..5** – pesos para cada componente RSI.
- **RSI Period 1..5** – períodos dos indicadores RSI.
- **Allow Buy / Allow Sell** – habilitar abertura de posições compradas ou vendidas.
- **Close Long / Close Short** – fechar posições existentes em sinais opostos.
- **Candle Type** – período dos candles processados (padrão 4 horas).

## Notas

A estratégia opera apenas em candles finalizados. A proteção de posição é iniciada automaticamente quando a estratégia começa.
