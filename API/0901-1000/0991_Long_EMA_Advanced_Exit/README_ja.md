# Long EMA 高度エグジット戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Long EMA 高度エグジット戦略は、短期移動平均が中期移動平均を上回るクロスが発生し、価格が長期移動平均の上にある場合にエントリーするロング専用戦略です。エグジットは、MACDの下向きクロス、選択した移動平均を下回る価格の終値、MAの下向きクロス、トレーリングストップ、またはATRベースのボラティリティフィルターによってトリガーされます。

## 詳細
- **データ**: 価格ローソク足。
- **エントリー条件**:
  - **ロング**: 短期MAが中期MAを上回るクロスし、価格が長期MAの上。
- **エグジット条件**: MACDの下向きクロス、選択したMAを下回る価格、短期MAが中期MAを下回るクロス、オプションのトレーリングストップ。
- **ストップ**: オプションのトレーリングストップ。
- **デフォルト値**:
  - `MaType` = EMA
  - `EntryConditionType` = Crossover
  - `LongTermPeriod` = 200
  - `ShortTermPeriod` = 5
  - `MidTermPeriod` = 10
  - `EnableMacdExit` = true
  - `MacdCandleType` = TimeSpan.FromDays(7).TimeFrame()
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
  - `UseTrailingStop` = false
  - `TrailingStopPercent` = 15
  - `UseMaCloseExit` = false
  - `MaCloseExitPeriod` = 50
  - `UseMaCrossExit` = true
  - `UseVolatilityFilter` = false
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 1.5
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: ロングのみ
  - インジケーター: MA, MACD, ATR
  - 複雑さ: 中
  - リスクレベル: 中
