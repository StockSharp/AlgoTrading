# Kevin Daveyによるケルトナーチャネル戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

シンプルなボラティリティチャネルシステムです。終値がケルトナーチャネルの下限バンドを下回るとロングエントリーし、終値が上限バンドを上回るとショートエントリーします。チャネルはEMAとATRの倍数から構築されます。

## デフォルトパラメーター
- `EmaPeriod` = 10
- `AtrPeriod` = 14
- `AtrMultiplier` = 1.6
- `CandleType` = 5 minute
