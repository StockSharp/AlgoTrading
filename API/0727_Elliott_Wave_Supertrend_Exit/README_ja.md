# Elliott Wave Supertrend エグジット戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

ZigZag状のリバーサルでエントリーし、Supertrendの方向転換または固定パーセンテージのストップロスでエグジットする戦略です。

## 詳細

- **エントリー条件**:
  - ロング: 価格がローカル安値を形成
  - ショート: 価格がローカル高値を形成
- **ロング/ショート**: 両方
- **エグジット条件**:
  - Supertrendの方向転換またはストップロスレベル
- **ストップ**: エントリー価格からの固定パーセンテージ
- **デフォルト値**:
  - `WaveLength` = 4
  - `SupertrendLength` = 10
  - `SupertrendMultiplier` = 3
  - `StopLossPercent` = 10
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: Highest, Lowest, SuperTrend
  - ストップ: あり
  - 複雑さ: 中級
  - 時間軸: 短期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
