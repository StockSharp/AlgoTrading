# Estratégia de Canal Keltner por Kevin Davey
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Um sistema simples de canal de volatilidade. Compra quando o fechamento cai abaixo da banda inferior do Canal Keltner e vende a descoberto quando o fechamento sobe acima da banda superior. O canal é construído a partir de uma EMA e um múltiplo de ATR.

## Parâmetros padrão
- `EmaPeriod` = 10
- `AtrPeriod` = 14
- `AtrMultiplier` = 1.6
- `CandleType` = 5 minute
