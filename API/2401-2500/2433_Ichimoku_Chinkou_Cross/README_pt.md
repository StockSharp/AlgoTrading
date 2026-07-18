# Estratégia de Cruzamento Ichimoku Chinkou
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera com base no cruzamento do Ichimoku Chinkou Span (linha atrasada) com o preço.

## Lógica da Estratégia

- **Comprado:** Chinkou cruza acima do preço, tanto o preço atual como Chinkou estão acima da nuvem Kumo, e o RSI está acima de `RsiBuyLevel`.
- **Vendido:** Chinkou cruza abaixo do preço, tanto o preço atual como Chinkou estão abaixo da nuvem Kumo, e o RSI está abaixo de `RsiSellLevel`.

A estratégia usa proteção de stop-loss via `StartProtection` e parâmetros para Tenkan, Kijun, Senkou Span B e RSI.

## Parâmetros

| Nome | Descrição | Padrão |
|------|-----------|--------|
| `TenkanPeriod` | Período do Tenkan-sen | 9 |
| `KijunPeriod` | Período do Kijun-sen | 26 |
| `SenkouSpanPeriod` | Período do Senkou Span B | 52 |
| `RsiPeriod` | Período de cálculo do RSI | 14 |
| `RsiBuyLevel` | RSI mínimo para comprado | 70 |
| `RsiSellLevel` | RSI máximo para vendido | 30 |
| `StopLoss` | Percentagem ou valor do stop-loss | 2% |
| `CandleType` | Tipo de vela para subscrição | Velas de 5 minutos |

## Indicadores

- Ichimoku
- RSI
