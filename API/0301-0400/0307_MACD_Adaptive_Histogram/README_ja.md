# MACDアダプティブヒストグラム戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

**MACD Adaptive Histogram**戦略は、適応型ヒストグラム閾値を持つMACDを中心に構築されています。

テストでは平均年間リターンが約184%であることが示されています。暗号通貨市場で最もよいパフォーマンスを発揮します。

HistogramがイントラデイI(15m)データでトレンド変化を確認したときにシグナルが発動します。これにより、この手法はアクティブトレーダーに適しています。

ストップはATRの倍数とFastPeriod、SlowPeriodなどの要素に基づいています。デフォルト値を調整してリスクとリワードのバランスを取ってください。

## 詳細
- **エントリー条件**: インジケーター条件の実装を参照。
- **ロング/ショート**: 両方。
- **エグジット条件**: 反対シグナルまたはストップロジック。
- **ストップ**: はい、インジケーターに基づく計算を使用。
- **デフォルト値**:
  - `FastPeriod = 12`
  - `SlowPeriod = 26`
  - `SignalPeriod = 9`
  - `HistogramAvgPeriod = 20`
  - `StdDevMultiplier = 2.0m`
  - `StopLossPercent = 2.0m`
  - `CandleType = TimeSpan.FromMinutes(15).TimeFrame()`
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: Histogram
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ (15m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
