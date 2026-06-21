# Keltner Channel Strategy by Kevin Davey
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

基于Keltner通道的简单策略。收盘价跌破下轨时做多，收盘价升破上轨时做空。通道由EMA和ATR倍数组成。

## 默认参数
- `EmaPeriod` = 10
- `AtrPeriod` = 14
- `AtrMultiplier` = 1.6
- `CandleType` = 5 分钟
