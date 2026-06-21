# ダブル AI SuperTrend トレーディング戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、トレンド方向を確認するために加重移動平均と組み合わせた2つのSuperTrendインジケーターを使用します。両方のSuperTrendが強気で価格WMAが対応するSuperTrend WMAの上方に留まるときにロングトレードが開かれます。逆の条件でショートトレードが発生します。ポジションは最初のSuperTrendからのATRベースのトレーリングストップで管理されます。

- **ロング**: 両方のSuperTrendが強気で価格WMAがSuperTrend WMAより上。
- **ショート**: 両方のSuperTrendが弱気で価格WMAがSuperTrend WMAより下。
- **インジケーター**: SuperTrend, WMA, ATR。
- **ストップ**: 最初のSuperTrendに基づくトレーリングストップ。
