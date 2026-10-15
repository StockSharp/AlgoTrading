# Quantum Sentiment Flux 戦略（初心者）
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、高速 EMA が低速 EMA を上回り、その差が ATR ベースの閾値を超えたときにロングエントリーします。逆のシグナルでショートエントリーします。価格が取引に対して ATR の倍数分動いた場合、または 2 倍の ATR の利益目標に達した場合にポジションをクローズします。クールダウン期間が取引頻度を制限します。

## パラメーター
- ローソク足タイプ
- 高速 EMA 期間
- 低速 EMA 期間
- ATR 期間
- ATR 乗数
- MA 強度閾値
- クールダウンバー
- 数量
