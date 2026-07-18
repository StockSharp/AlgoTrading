# プルバック Pro Dow戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

ダウ理論のピボットを使ってトレンド方向を定義し、ADXによりトレンド強度が確認された際にEMAへのプルバックでエントリーします。システムは2つのリスク・リワード目標でスケールアウトします。

バックテストでは、US30などの株価指数先物で安定した動作が確認されています。

## 詳細

- **エントリー条件**:
  - ロング: 高値と安値が切り上がり、安値がEMAを下抜け、ADXが閾値を上回る
  - ショート: 高値と安値が切り下がり、高値がEMAを上抜け、ADXが閾値を上回る
- **ロング/ショート**: 両方
- **エグジット条件**: 直近ピボットにストップ、2つのR:R目標で利益確定
- **ストップ**: ピボットベース
- **デフォルト値**:
  - `PivotLookback` = 10
  - `EmaLength` = 21
  - `RiskReward1` = 1.5m
  - `Tp1Percent` = 50
  - `RiskReward2` = 3m
  - `UseAdxFilter` = true
  - `AdxLength` = 14
  - `AdxThreshold` = 25m
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: EMA, Average Directional Index
  - ストップ: あり
  - 複雑さ: 中級
  - 時間軸: 短期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
