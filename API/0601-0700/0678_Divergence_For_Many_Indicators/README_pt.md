# Estratégia de Divergência para Múltiplos Indicadores
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Detecta divergências de alta e de baixa entre o preço e o RSI e o histograma do MACD. Quando o número de divergências atinge o limiar especificado, a estratégia entra em uma operação na direção oposta.

## Parâmetros
- `RsiPeriod` – período para o cálculo do RSI.
- `MacdFastPeriod` – período rápido para o MACD.
- `MacdSlowPeriod` – período lento para o MACD.
- `MacdSignalPeriod` – período de sinal para o MACD.
- `MinDivergence` – mínimo de indicadores que confirmam a divergência.
- `CandleType` – tipo de vela para assinatura.
