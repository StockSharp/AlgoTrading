# テクニカルランク（戦略）
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は移動平均、変化率、PPOの傾き、RSIから複合テクニカルランクを算出します。ランクが上限閾値を超えるとロングポジションを開き、下限閾値を下回るとショートポジションを開きます。

## 詳細

- **エントリー条件**: ランク > UpperThreshold → ロング；ランク < LowerThreshold → ショート。
- **ロング/ショート**: 両方。
- **エグジット条件**: 逆シグナル。
- **ストップ**: いいえ。
- **デフォルト値**:
  - `UpperThreshold` = 70
  - `LowerThreshold` = 30
  - `CandleType` = 1分足ロウソク足
