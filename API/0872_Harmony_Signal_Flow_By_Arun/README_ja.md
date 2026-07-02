# Harmony Signal Flow By Arun 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Harmony Signal Flow By Arunは短期RSIを使って、固定のストップロスと目標レベルで転換点を捉えます。RSIが下限閾値を上抜けするとロング、上限閾値を下抜けするとショートに入ります。ポジションはストップ・目標到達、または毎日15:25にクローズされます。

## 詳細
- **データ**: 価格ローソク足。
- **エントリー条件**:
  - **ロング**: RSIが`LowerThreshold`を上抜け。
  - **ショート**: RSIが`UpperThreshold`を下抜け。
- **エグジット条件**: ストップロスまたは目標到達、あるいは15:25のクローズ。
- **ストップ**: 固定ストップロスと目標。
- **デフォルト値**:
  - `RsiPeriod` = 5
  - `LowerThreshold` = 30
  - `UpperThreshold` = 70
  - `BuyStopLoss` = 100
  - `BuyTarget` = 150
  - `SellStopLoss` = 100
  - `SellTarget` = 150
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: ロング & ショート
  - インジケーター: RSI
  - 複雑さ: 低
  - リスクレベル: 中
