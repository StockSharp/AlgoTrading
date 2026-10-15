# Estratégia RSI Stochastic MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia combina um filtro de tendência de média móvel simples (SMA) com os osciladores RSI e Stochastic.
A média móvel define o viés do mercado. Quando o preço está acima da SMA, a estratégia busca entradas compradas;
quando está abaixo da SMA, busca entradas vendidas. Os níveis de RSI e Stochastic identificam condições de sobrevenda ou
sobrecompra para sincronizar as entradas.

As posições são fechadas quando os osciladores saem de suas zonas extremas. Isso mantém as operações alinhadas com
a tendência predominante, evitando movimentos prolongados contra os indicadores.

## Parâmetros
- `RsiPeriod` – período de cálculo do RSI.
- `RsiUpperLevel` – limiar de sobrecompra do RSI.
- `RsiLowerLevel` – limiar de sobrevenda do RSI.
- `MaPeriod` – período da média móvel de tendência.
- `StochKPeriod` – período %K do oscilador Stochastic.
- `StochDPeriod` – período de suavização %D do oscilador Stochastic.
- `StochUpperLevel` – nível de sobrecompra do Stochastic.
- `StochLowerLevel` – nível de sobrevenda do Stochastic.
- `Volume` – volume da ordem.
- `CandleType` – tipo de dados de velas usado para cálculos.

## Indicadores
- Média Móvel Simples
- Índice de Força Relativa
- Oscilador Stochastic

## Regras de trading
- **Comprar** quando o preço está acima da SMA, RSI está abaixo de `RsiLowerLevel` e ambas as linhas do Stochastic estão abaixo de `StochLowerLevel`.
- **Vender** quando o preço está abaixo da SMA, RSI está acima de `RsiUpperLevel` e ambas as linhas do Stochastic estão acima de `StochUpperLevel`.
- **Fechar comprado** quando RSI ou Stochastic sobe acima de seus níveis superiores.
- **Fechar vendido** quando RSI ou Stochastic cai abaixo de seus níveis inferiores.
