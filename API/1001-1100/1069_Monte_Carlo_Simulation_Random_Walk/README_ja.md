# Monte Carlo シミュレーション - ランダムウォーク戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

このサンプル戦略は、過去の対数リターンを用いて将来の価格パスのMonte Carloシミュレーションを実行します。取引は行わず、ランダムウォークを生成して将来の最高値・最安値の水準を推定する方法を実演します。

## 詳細

- **エントリー条件**: なし。この戦略は取引を行いません。
- **ロング/ショート**: なし。
- **エグジット条件**: 該当なし。
- **ストップ**: なし。
- **デフォルト値**:
  - `NumberOfBarsToPredict` = 50.
  - `NumberOfSimulations` = 500.
  - `DataLength` = 2000.
  - `KeepPastMinMaxLevels` = false.
- **フィルター**: 該当なし。
- **複雑さ**: 中程度。
- **時間軸**: 設定可能。

