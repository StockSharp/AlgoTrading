# Varanormal Mac N Cheez戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

トレーリングストップと日次利益目標を備えたSMAクロスオーバー戦略。

## 詳細

- **エントリー条件**:
  - **ロング**: 高速SMAが低速SMAを上抜け。
  - **ショート**: 高速SMAが低速SMAを下抜け。
- **ロング/ショート**: 両方向。
- **エグジット条件**:
  - トレーリングストップまたは固定ストップロス。
  - 日次利益目標に達したら全ポジションをクローズ。
- **ストップ**: はい、固定とトレーリング。
- **デフォルト値**:
  - `FastLength` = 9
  - `SlowLength` = 21
  - `DailyTarget` = 200
  - `StopLossAmount` = 100
  - `TrailOffset` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: SMA
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
