# Awesome Osc Trader戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、Bollinger Band の幅、stochastic フィルター、正規化された Awesome Oscillator momentum チェックを組み合わせ、MetaTrader エキスパート "Awesome Osc Trader" を再現します。ロング取引は、オシレーターが負の極値から上昇し、stochastic が売られ過ぎ領域を離れ、市場ボラティリティが設定可能なバンド幅内に留まると開かれます。ショートには反転条件が必要です。設定可能な取引窓は新規注文を特定の時間に限定し、オープンポジションは含み益が選択したフィルターに一致する場合にのみ反対シグナルで強制決済できます。

## 詳細

- **エントリー条件**:
  - pips に変換された Bollinger Band スプレッドは、`BollingerSpreadLowerLimit` と `BollingerSpreadUpperLimit` の間に留まる必要があります。
  - stochastic メインラインは、ロングでは `StochLower` より上、ショートでは `StochUpper` より下です。
  - 正規化された Awesome Oscillator は、ゼロの反対側で少なくとも 4 本連続バーを示し、`AoStrengthLimit` を超える強さでゼロへ戻り始めています。
  - 現在時刻は `EntryHour` と `OpenHours` で定義される取引窓内です。
- **ロング/ショート**: 両方向を取引します。
- **エグジット条件**:
  - 反対シグナルが現れたとき、またはオシレーターがゼロをクロスしたときの任意の早期エグジット。`CloseTrade` と `ProfitTypeClTrd` で制御されます。
  - pips で指定される保護 stop-loss、take-profit、trailing stop 距離。
- **ストップ**: `StartProtection` で管理される固定ストップ、take-profit、任意の trailing stop。
- **デフォルト値**:
  - `BollingerPeriod` = 20, `BollingerSigma` = 2
  - `BollingerSpreadLowerLimit` = 55, `BollingerSpreadUpperLimit` = 380
  - `PeriodFast` = 3, `PeriodSlow` = 32
  - `AoStrengthLimit` = 0.13
  - `StochK` = 8, `StochD` = 3, `StochSlow` = 3
  - `StochLower` = 18, `StochUpper` = 76
  - `EntryHour` = 0, `OpenHours` = 16
  - `Lots` = 0.01, `TakeProfit` = 200, `StopLoss` = 80, `TrailingStop` = 40
  - `CloseTrade` = true, `ProfitTypeClTrd` = 1 (利益のあるポジションのみ閉じる)
- **フィルター**:
  - カテゴリ: ボラティリティフィルター付きMomentum
  - 方向: ロングとショート
  - インジケーター: Bollinger Bands, Stochastic Oscillator, Awesome Oscillator
  - ストップ: はい (固定とtrailing)
  - 複雑さ: 中
  - 時間軸: H1 向けに設計されていますが、任意のローソク足系列で動作します
  - 季節性: 取引時間窓
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中程度
