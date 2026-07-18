# Estratégia Supertrend Ponderada por Volume
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia calcula um Supertrend baseado em uma média móvel ponderada por volume e uma banda ATR. Um segundo Supertrend é aplicado ao volume para confirmar a força da tendência. Uma posição comprada é aberta quando as tendências de volume e preço se alinham para cima, e fechada quando as condições se revertem.

## Parâmetros
- **ATR Period** – período ATR para a tendência de preço.
- **Volume Period** – período para VWAP e tendência de volume.
- **Factor** – multiplicador ATR.
- **Candle Type** – período dos candles processados.
