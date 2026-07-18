# 収益性の高いSuperTrend + MA + Stoch戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

SuperTrend、移動平均クロスオーバー、Stochasticオシレーターを組み合わせた戦略。

SuperTrendで特定されたトレンドを捉え、EMAクロスとStochasticレベルでエントリーを確認します。オプションの利確・損切り目標を含みます。

## 詳細

- **エントリー条件**: SuperTrendによるトレンド、EMAクロスオーバー、Stochastic閾値。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 反対方向のEMAクロスオーバーまたはTP/SL。
- **ストップ**: あり。
- **デフォルト値**:
  - `AtrPeriod` = 10
  - `AtrMultiplier` = 3m
  - `MaFastPeriod` = 9
  - `MaSlowPeriod` = 21
  - `StochKPeriod` = 14
  - `StochDPeriod` = 3
  - `TakeProfitPercent` = 2m
  - `StopLossPercent` = 1m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: SuperTrend, EMA, Stochastic
  - ストップ: あり
  - 複雑さ: 基本
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
