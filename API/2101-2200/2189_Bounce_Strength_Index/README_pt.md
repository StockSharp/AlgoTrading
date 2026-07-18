# Estratégia de Índice de Força de Reversão
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia implementa uma versão simplificada do Bounce Strength Index (BSI). O indicador mede como o preço fecha dentro de uma faixa recente e aplica suavização dupla para destacar as mudanças de momentum.

## Lógica
- Calcular os preços máximos e mínimos recentes usando os indicadores **Highest** e **Lowest**.
- Determinar a posição do fechamento dentro dessa faixa e suavizar o resultado duas vezes com **SimpleMovingAverage**.
- Quando o indicador vira para cima, as posições vendidas são fechadas e uma posição comprada é aberta.
- Quando o indicador vira para baixo, as posições compradas são fechadas e uma posição vendida é aberta.

## Parâmetros
- `CandleType` – série de velas utilizada para análise.
- `RangePeriod` – período de retrospecto para o cálculo da faixa.
- `Slowing` – comprimento da suavização rápida.
- `AvgPeriod` – comprimento da suavização lenta.

## Indicadores
- BounceStrengthIndex (personalizado)
- Highest
- Lowest
- SimpleMovingAverage
