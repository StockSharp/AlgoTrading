# トレーリングモンスター戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

KAMAトレンド検出とRSIフィルター、トレーリングストップを組み合わせた戦略。RSIがKAMAトレンドの方向に極値を越えたときにポジションを開設します。遅延後、パーセンテージトレーリングストップが利益を保護します。

## 詳細
- **エントリー条件**:
  - **ロング**: RSI > `RsiOverbought`、終値がSMAを上回り、KAMAが上昇中
  - **ショート**: RSI < `RsiOversold`、終値がSMAを下回り、KAMAが下降中
- **ロング/ショート**: 両方
- **エグジット条件**:
  - `DelayBars`後のパーセンテージトレーリングストップ
- **ストップ**: パーセンテージトレーリングストップ
- **デフォルト値**:
  - `KamaLength` = 40
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `SmaLength` = 200
  - `BarsBetweenEntries` = 3
  - `TrailingStopPct` = 12m
  - `DelayBars` = 3
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: KAMA, RSI, SMA
  - ストップ: トレーリング
  - 複雑さ: 基本
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
