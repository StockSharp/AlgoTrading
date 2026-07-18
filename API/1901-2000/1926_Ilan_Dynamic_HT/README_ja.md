# Ilan Dynamic HT戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

RSIシグナルに基づいてポジションを開き、動的な価格レンジを使ってポジションを拡大するグリッドベースのMartingale戦略です。追加トレードごとに乗数でボリュームが増加し、同じテイクプロフィットとストップロスを共有します。

## 詳細

- **エントリー条件**:
  - ロング: RSIが`RsiMinimum`を下回る
  - ショート: RSIが`RsiMaximum`を上回る
- **ロング/ショート**: ロングとショート
- **エグジット条件**:
  - 共通のテイクプロフィットまたはストップロスに達する
- **ストップ**:
  - `TakeProfit`（ポイント単位）
  - `StopLoss`（ポイント単位）
- **デフォルト値**:
  - `LotExponent` = 1.4
  - `MaxTrades` = 10
  - `DynamicPips` = true
  - `DefaultPips` = 120
  - `Depth` = 24
  - `Del` = 3
  - `BaseVolume` = 0.1
  - `RsiPeriod` = 14
  - `RsiMinimum` = 30
  - `RsiMaximum` = 70
  - `TakeProfit` = 100
  - `StopLoss` = 500
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **フィルター**:
  - カテゴリ: グリッド / Martingale
  - 方向: ロングとショート
  - インジケーター: RSI, Highest, Lowest
  - ストップ: テイクプロフィット, ストップロス
  - 複雑さ: 上級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 高
