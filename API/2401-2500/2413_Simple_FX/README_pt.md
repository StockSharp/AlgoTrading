# Estratégia Simple FX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia usa duas médias móveis exponenciais para detectar mudanças de tendência. Uma posição comprada é aberta quando a EMA curta cruza acima da EMA longa, enquanto uma posição vendida é aberta quando a EMA curta cruza abaixo da EMA longa.

## Parâmetros
- **Long MA Period** – período da EMA longa.
- **Short MA Period** – período da EMA curta.
- **Stop Loss (points)** – stop de proteção em passos de preço.
- **Take Profit (points)** – alvo de lucro em passos de preço.
- **Candle Type** – período para os candles.
