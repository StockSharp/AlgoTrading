# Flash Minerviniクオリファイアー戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

EMAクロスオーバー、SuperTrendおよびモメンタムRSIをMinerviniステージ分析と組み合わせてトレードを選別します。

## 詳細

- **エントリー条件**: EMAがトレーリングラインを上回り、SuperTrendのトレンドが確認され、モメンタムRSIが閾値を超え、Minerviniステージフィルターを満たす
- **ロング/ショート**: 両方
- **エグジット条件**: 反対のトレーリングまたはSuperTrendの反転
- **ストップ**: いいえ
- **デフォルト値**:
  - `MomRsiLength` = 10
  - `MomRsiThreshold` = 60
  - `EmaLength` = 12
  - `EmaPercent` = 0.01
  - `SuperTrendPeriod` = 10
  - `SuperTrendMultiplier` = 3
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: EMA, SuperTrend, RSI
  - ストップ: いいえ
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
