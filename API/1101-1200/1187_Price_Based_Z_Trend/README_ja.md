# Price Based Z-Trend戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

EMAに対する価格のZ-scoreに基づいて取引します。Z-scoreがユーザー定義の閾値を超えたときにエントリーし、ロング・ショート・両方向をサポートします。

## 詳細

- **エントリー条件**:
  - Z-scoreが`Threshold`を上回るとロング。
  - Z-scoreが`-Threshold`を下回るとショート。
- **ロング/ショート**: `TradeDirection`で設定可能。
- **エグジット条件**: 反対側の閾値クロスオーバー。
- **ストップ**: なし。
- **デフォルト値**:
  - `PriceDeviationLength` = 100
  - `PriceAverageLength` = 100
  - `Threshold` = 1
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 設定可能
  - インジケーター: EMA, StandardDeviation
  - ストップ: なし
  - 複雑さ: 基本
  - 時間軸: 5分
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
