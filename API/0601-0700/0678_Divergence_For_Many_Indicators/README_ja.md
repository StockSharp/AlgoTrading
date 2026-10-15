# 複数インジケーターのダイバージェンス戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

価格とRSIおよびMACDヒストグラムの間の強気・弱気ダイバージェンスを検出します。ダイバージェンスの数が指定の閾値に達すると、戦略は反対方向に取引を開始します。

## パラメーター
- `RsiPeriod` – RSI計算の期間。
- `MacdFastPeriod` – MACDの速い期間。
- `MacdSlowPeriod` – MACDの遅い期間。
- `MacdSignalPeriod` – MACDのシグナル期間。
- `MinDivergence` – ダイバージェンスを確認するインジケーターの最小数。
- `CandleType` – サブスクリプション用のローソク足タイプ。
