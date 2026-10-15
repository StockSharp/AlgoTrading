# Estratégia MultiCamada Awesome Oscillator Saucer
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Implementa uma estratégia altista de múltiplas camadas baseada no padrão saucer do Awesome Oscillator e na detecção de tendência por fractais. A estratégia conta sinais saucer consecutivos e coloca até cinco ordens de compra stop escalonadas acima do preço. As posições são fechadas quando a tendência se reverte.

## Parâmetros
- **EMA Length** – período do filtro EMA.
- **Candle Type** – tipo de velas.
- **Trade Start** – início do período de negociação.
- **Trade Stop** – fim do período de negociação.
