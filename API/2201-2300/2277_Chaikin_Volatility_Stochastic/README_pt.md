# Estratégia de Volatilidade Chaikin Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia aplica um oscilador estocástico aos valores de volatilidade de Chaikin para capturar reversões de tendência. O intervalo máximo-mínimo de cada vela é suavizado com uma EMA, depois normalizado com um cálculo estocástico e finalmente suavizado por uma média móvel ponderada.

Quando o oscilador suavizado vira descendente após subir, uma posição comprada é aberta e qualquer posição vendida é fechada. Quando o oscilador vira ascendente após cair, uma posição vendida é aberta e qualquer posição comprada é fechada.

## Parâmetros
- **Candle Type**: período para subscrição de velas.
- **EMA Length**: período de suavização para o intervalo máximo-mínimo.
- **Stochastic Length**: período de lookback para o cálculo estocástico.
- **WMA Length**: período de média móvel ponderada para suavizar o oscilador.
- **Enable Longs / Enable Shorts**: alternar direções de negociação permitidas.

## Indicadores
- ExponentialMovingAverage
- Highest e Lowest
- WeightedMovingAverage

## Regras de Negociação
- **Entrada comprada**: o oscilador estava subindo e vira descendente.
- **Entrada vendida**: o oscilador estava caindo e vira ascendente.
- Posições opostas são fechadas na mudança de sinal.
