# アダプティブBollingerブレイクアウト戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
**Adaptive Bollinger Breakout**戦略は、適応的に調整されたパラメーターを持つボリンジャーバンドのブレイクアウトに基づいて取引します。

BollingerがイントラデイI（5m）データのブレイクアウト機会を確認したときにシグナルが発生します。このため、アクティブトレーダーに適した手法です。

ストップはATRの倍数とMinBollingerPeriod、MaxBollingerPeriodなどのパラメーターに基づいています。リスクとリワードのバランスを取るためにこれらのデフォルト値を調整してください。

## 詳細
- **エントリー条件**: インジケーターの条件については実装を参照してください。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 反対のシグナルまたはストップロジック。
- **ストップ**: はい、インジケーターベースの計算を使用。
- **デフォルト値**:
  - `MinBollingerPeriod = 10`
  - `MaxBollingerPeriod = 30`
  - `MinBollingerDeviation = 1.5m`
  - `MaxBollingerDeviation = 2.5m`
  - `AtrPeriod = 14`
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
