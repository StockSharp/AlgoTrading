# Adaptive Renko戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、**Average True Range（ATR）**インジケーターで測定される市場のボラティリティにレンガサイズが追従するアダプティブRenkoグリッドを構築します。価格がどちらかの方向に1レンガ分移動するたびにトレードが実行されます。

## ロジック
- ATRは設定可能な`VolatilityPeriod`で計算されます。
- レンガサイズは`ATR * Multiplier`に等しいですが、`MinBrickSize`未満にはなりません。
- 価格が前のレンガを少なくとも1レンガ分上回ると、戦略は買い（必要に応じてショートポジションを決済）ます。
- 価格が前のレンガを少なくとも1レンガ分下回ると、戦略は売り（必要に応じてロングポジションを決済）ます。

## パラメーター
- `Volume` – 注文数量。
- `VolatilityPeriod` – ATR計算に使用する期間。
- `Multiplier` – ATRに適用する係数。
- `MinBrickSize` – 価格単位での最小レンガサイズ。
- `CandleType` – ATR計算に使用する時間軸。

## 時間軸
- デフォルト：4時間。
