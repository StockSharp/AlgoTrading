# レンジフィルター戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

リアルなレンジ計算と固定のリスク/リワードレベルを使用したレンジフィルター戦略です。

平滑化されたレンジを使用して価格周辺にダイナミックなバンドを形成します。価格がこれらのバンドを上下に突破したときにトレードを行います。リスク管理には固定の損切り・利確距離を使用します。

## 詳細

- **エントリー条件**: 価格がレンジフィルターのバンドを突破する。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 損切りまたは利確。
- **ストップ**: はい。
- **デフォルト値**:
  - `SamplingPeriod` = 100
  - `RangeMultiplier` = 3
  - `RiskPoints` = 50
  - `RewardPoints` = 100
  - `MaxTradesPerDay` = 5
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: Range filter
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
