# カラーコードオーバーレイ戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

カスタムカラーコード計算を使用したローソク足の色変化で取引し、固定 pip ベースのストップを適用します。

## ロジック
- OHLC 値からカスタムカラーコードのローソク足を構築する。
- ローソク足の実体がレンジの 1% を超えたときに色の切り替えを検出する。
- 取引タイプに従い、赤から緑への転換でロング、緑から赤への転換でショートを建てる。
- `StartTime` から `EndTime` の間のみ動作する。
- `StopLossPips` と `TakeProfitPips` の保護を適用する。
