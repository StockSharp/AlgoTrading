# 下落平均戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、価格がEMA周辺のATRベースのバンドの外側に移動したときにポジションを開きます。市場がポジションに逆行した場合、戦略はステップスケールの割合偏差（DCA）を使用してポジションに追加します。価格が平均エントリーに固定パーセントを加えた水準に戻ったときに利益を確定します。

## パラメーター
- Candle Type – 処理するローソク足の種類。
- EMA Length – EMAトレンドフィルターの期間。
- ATR Length – ATRの期間。
- ATR Mult – ATRバンドの乗数。
- TP % – 平均エントリーからの利益確定パーセンテージ。
- Base Deviation % – 最初のDCAレベルの初期偏差。
- Step Scale – 各新しいDCAレベルの偏差に適用される乗数。
- DCA Size Multiplier – 各DCA注文のボリューム乗数。
- Max DCA Levels – 平均化エントリーの最大数。
- Initial Volume – 最初の注文のボリューム。
