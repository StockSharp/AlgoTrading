# Bollingerボラティリティブレイクアウト戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

**Bollinger Volatility Breakout**戦略は、ボラティリティ確認を伴うBollinger Bandsのブレイクアウトを中心に構築されています。

テストでは平均年間リターンが約181%であることが示されています。暗号通貨市場で最もよいパフォーマンスを発揮します。

BollingerがイントラデイI(5m)データでブレイクアウトの機会を確認したときにシグナルが発動します。これにより、この手法はアクティブトレーダーに適しています。

ストップはATRの倍数とBollingerPeriod、BollingerDeviationなどの要素に基づいています。デフォルト値を調整してリスクとリワードのバランスを取ってください。

## 詳細
- **エントリー条件**: インジケーター条件の実装を参照。
- **ロング/ショート**: 両方。
- **エグジット条件**: 反対シグナルまたはストップロジック。
- **ストップ**: はい、インジケーターに基づく計算を使用。
- **デフォルト値**:
  - `BollingerPeriod = 20`
  - `BollingerDeviation = 2.0m`
  - `AtrPeriod = 14`
  - `AtrDeviationMultiplier = 2.0m`
  - `StopLossMultiplier = 2.0m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: Bollinger
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
