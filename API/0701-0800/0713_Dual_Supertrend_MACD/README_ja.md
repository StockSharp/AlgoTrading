# デュアル Supertrend MACD 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

デュアル Supertrend MACD 戦略は、2つの Supertrend インジケーターと MACD フィルターを組み合わせます。
価格が両方の Supertrend ラインより上にあり、MACD ヒストグラムが正の場合にロングポジションを建てます。
価格が両方のラインより下にあり、ヒストグラムが負の場合にショートポジションが発生します。
いずれかの Supertrend が方向を転換するか、MACD ヒストグラムがゼロを越えたときにポジションを閉じます。

## 詳細
- **データ**: 価格ローソク足。
- **エントリー条件**:
  - ロング: `Close > Supertrend1 && Close > Supertrend2 && MACD Histogram > 0`
  - ショート: `Close < Supertrend1 && Close < Supertrend2 && MACD Histogram < 0`
- **エグジット条件**:
  - ロング: `Close < Supertrend1 || Close < Supertrend2 || MACD Histogram < 0`
  - ショート: `Close > Supertrend1 || Close > Supertrend2 || MACD Histogram > 0`
- **ストップ**: デフォルトなし。
- **デフォルト値**:
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `OscillatorMaType` = Exponential
  - `SignalMaType` = Exponential
  - `AtrPeriod1` = 10
  - `Factor1` = 3.0
  - `AtrPeriod2` = 20
  - `Factor2` = 5.0
  - `TradeDirection` = "Both"
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 設定可能
  - インジケーター: Supertrend, MACD
  - 複雑さ: 中級
  - リスクレベル: 中
