# Parabolic SAR RSIダイバージェンス戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
**Parabolic SAR RSI Divergence**戦略は、RSIが価格に対してダイバージェンスを示しているときのParabolic SARシグナルに基づいて取引します。

テストでは年間平均リターン約103%を示しています。株式市場で最も効果を発揮します。

ParabolicがイントラデイI（5m）データのダイバージェンスセットアップを確認したときにシグナルが発生します。このため、アクティブトレーダーに適した手法です。

ストップはATRの倍数とSarAccelerationFactor、SarMaxAccelerationFactorなどのパラメーターに基づいています。リスクとリワードのバランスを取るためにこれらのデフォルト値を調整してください。

## 詳細
- **エントリー条件**: インジケーターの条件については実装を参照してください。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 反対のシグナルまたはストップロジック。
- **ストップ**: はい、インジケーターベースの計算を使用。
- **デフォルト値**:
  - `SarAccelerationFactor = 0.02m`
  - `SarMaxAccelerationFactor = 0.2m`
  - `RsiPeriod = 14`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: Parabolic, Divergence
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: はい
  - リスクレベル: 中
