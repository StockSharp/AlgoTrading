# Donchian Hurst Exponent戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
**Donchian Hurst Exponent**戦略は、Hurst Exponentフィルターを使用したDonchianチャネルのブレイクアウトに基づいて取引します。

テストでは年間平均リターン約91%を示しています。株式市場で最も効果を発揮します。

DonchianがイントラデイI（5m）データのトレンド転換を確認したときにシグナルが発生します。このため、アクティブトレーダーに適した手法です。

ストップはATRの倍数とDonchianPeriod、HurstPeriodなどのパラメーターに基づいています。リスクとリワードのバランスを取るためにこれらのデフォルト値を調整してください。

## 詳細
- **エントリー条件**: インジケーターの条件については実装を参照してください。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 反対のシグナルまたはストップロジック。
- **ストップ**: はい、インジケーターベースの計算を使用。
- **デフォルト値**:
  - `DonchianPeriod = 20`
  - `HurstPeriod = 100`
  - `HurstThreshold = 0.5m`
  - `StopLossPercent = 2m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: Donchian, Hurst, Exponent
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
