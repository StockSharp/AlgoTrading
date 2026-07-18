# SuperTrend SDI Webhook 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

SuperTrend と平滑化方向性指標 (SDI) に基づく戦略。+DI が -DI を上回り SuperTrend が上昇トレンドを示すときにロングエントリーします。-DI が +DI を上回り SuperTrend が下向きを示すときにショートポジションを建てます。パーセントベースのテイクプロフィット、ストップロス、トレーリングストップを適用します。

## 詳細

- **エントリー条件**:
  - ロング: `+DI > -DI && SuperTrend up`
  - ショート: `-DI > +DI && SuperTrend down`
- **ロング/ショート**: 両方
- **エグジット条件**: テイクプロフィット、ストップロスまたはトレーリングストップ
- **インジケーター**: SuperTrend, AverageDirectionalIndex
- **ストップ**: パーセントベースのテイクプロフィット、ストップロス、トレーリングストップ
- **デフォルト値**:
  - `AtrPeriod` = 10
  - `AtrMultiplier` = 1.8m
  - `DiLength` = 3
  - `DiSmooth` = 7
  - `TakeProfitPercent` = 25m
  - `StopLossPercent` = 4.8m
  - `TrailingPercent` = 1.9m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: SuperTrend, SDI
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
