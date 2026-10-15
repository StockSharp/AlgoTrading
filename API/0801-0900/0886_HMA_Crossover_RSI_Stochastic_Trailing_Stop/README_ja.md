# HMA クロスオーバー RSI Stochastic トレーリングストップ戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

高速・低速HMAのクロスオーバーとRSIおよびスムーズStochasticフィルターを使用した戦略です。高速HMAが低速HMAを上抜けし、RSIとStochasticが閾値を下回るときにロングをオープンし、逆の条件でショートをオープンします。トレーリングストップがエグジットを管理します。

## 詳細

- **エントリー条件**: RSIとStochasticが閾値未満の状態で高速HMAが低速HMAを上抜け。
- **ロング/ショート**: 両方向。
- **エグジット条件**: トレーリングストップまたは逆シグナル。
- **ストップ**: 割合トレーリング。
- **デフォルト値**:
  - `FastHmaLength` = 5
  - `SlowHmaLength` = 20
  - `RsiPeriod` = 14
  - `RsiBuyLevel` = 45
  - `RsiSellLevel` = 60
  - `StochLength` = 14
  - `StochSmooth` = 3
  - `StochBuyLevel` = 39
  - `StochSellLevel` = 63
  - `TrailingPercent` = 5
  - `CandleType` = TimeSpan.FromHours(1)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: HMA, RSI, Stochastic
  - ストップ: トレーリング
  - 複雑さ: 基本
  - 時間軸: 1h
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
