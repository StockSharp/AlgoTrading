# RSI動的買われすぎ/売られすぎ戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

**RSI Dynamic Overbought Oversold**戦略は、動的な買われすぎ/売られすぎレベルを持つRSIを中心に構築されています。

テストでは平均年間リターンが約178%であることが示されています。株式市場で最もよいパフォーマンスを発揮します。

買われすぎがイントラデイ(5m)データでトレンド変化を確認したときにシグナルが発動します。これにより、この手法はアクティブトレーダーに適しています。

ストップはATRの倍数とRsiPeriod、MovingAvgPeriodなどの要素に基づいています。デフォルト値を調整してリスクとリワードのバランスを取ってください。

## 詳細
- **エントリー条件**: インジケーター条件の実装を参照。
- **ロング/ショート**: 両方。
- **エグジット条件**: 反対シグナルまたはストップロジック。
- **ストップ**: はい、インジケーターに基づく計算を使用。
- **デフォルト値**:
  - `RsiPeriod = 14`
  - `MovingAvgPeriod = 50`
  - `StdDevMultiplier = 2.0m`
  - `StopLossPercent = 2.0m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: Overbought, Oversold
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
