# Bias Sentiment Strength

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Regras de negociação

Em velas concluídas de 15 minutos, combinam-se MACD(12,26,9), RSI Wilder(14), estocástico(21,14,14), Awesome Oscillator(5,34), Alligator e viés por volume. K bruto usa extremos de 21 barras; `Ks` é sua SMA de 14 valores e `D` outra SMA de 14. Alligator usa SMMAs do preço mediano: mandíbula 13 deslocada 8, dentes 8 deslocados 5 e lábios 5 deslocados 3.

`BASS = 2 * (MACD - Signal) + (RSI - 50) / 5 + 0.15 * (Ks - D) + 0.6 * AO + (Lips - Teeth) + (Teeth - Jaw) + VWMA(30) - SMA(30)`. Todos os componentes devem estar formados; por padrão são necessárias pelo menos 47 barras completas. Configuração inválida falha explicitamente.

Acima de `BassThreshold`, o alvo é comprado `Volume`; abaixo de `-BassThreshold`, vendido `Volume`. O limiar padrão é zero. Usam-se níveis, permitindo nova entrada após stop sem novo cruzamento do zero. A reversão usa `Volume + abs(Position)`; ordem pendente bloqueia outra. O stop nativo local a mercado fecha toda a exposição executada a `StopLossPercent` por cento, padrão 1. Não há take-profit.

A proteção local nativa recebe velas e atualizações de preço assinadas do instrumento. As distâncias partem das execuções reais de entrada. As saídas são a mercado; o preço depende do mercado disponível e do passo de preço.

## Parâmetros

| Parâmetro | Padrão |
|---|---|
| `MacdFast` | `12` |
| `MacdSlow` | `26` |
| `MacdSignal` | `9` |
| `RsiPeriod` | `14` |
| `StochK` | `21` |
| `StochSmooth` | `14` |
| `StochD` | `14` |
| `AoShort` | `5` |
| `AoLong` | `34` |
| `VolumeLength` | `30` |
| `BassThreshold` | `0` |
| `StopLossPercent` | `1` |
| `CandleType` | `15m` |
