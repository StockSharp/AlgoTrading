# MACD Volume Cluster戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
**MACD Volume Cluster**戦略は、MACDボリュームクラスターの分析を基盤としています。

インジケーターがイントラデイ（5m）データのトレンド転換を確認したときにシグナルが発生します。このため、アクティブトレーダーに適した手法です。

ストップはATRの倍数とFastMacdPeriod、SlowMacdPeriodなどのパラメーターに基づいています。リスクとリワードのバランスを取るためにこれらのデフォルト値を調整してください。

## 詳細
- **エントリー条件**: インジケーターの条件については実装を参照してください。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 反対のシグナルまたはストップロジック。
- **ストップ**: はい、インジケーターベースの計算を使用。
- **デフォルト値**:
  - `FastMacdPeriod = 12`
  - `SlowMacdPeriod = 26`
  - `MacdSignalPeriod = 9`
  - `VolumePeriod = 20`
  - `VolumeDeviationFactor = 2.0m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: 複数のインジケーター
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
