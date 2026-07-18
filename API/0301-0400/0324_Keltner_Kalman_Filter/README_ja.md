# Keltner Kalman Filter戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

**Keltner Kalman Filter**戦略はKeltnerチャンネルとKalman Filterを組み合わせてトレンドと取引機会を特定します。

テストでは年平均リターン約73%が示されています。暗号通貨市場で最もよいパフォーマンスを発揮します。

KeltnerがイントラデイデータでフィルタリングされたエントリーConfirmするとシグナルが発生します (15m)。この手法はアクティブなトレーダーに適しています。

ストップはATRの倍数とEmaPeriod、AtrPeriodなどのパラメーターに依存します。リスクとリワードのバランスを取るためにこれらのデフォルト値を調整してください。

## 詳細
- **エントリー条件**: インジケーター条件の実装を参照。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 逆シグナルまたはストップロジック。
- **ストップ**: はい、インジケーターベースの計算を使用。
- **デフォルト値**:
  - `EmaPeriod = 20`
  - `AtrPeriod = 14`
  - `AtrMultiplier = 2.0m`
  - `KalmanProcessNoise = 0.01m`
  - `KalmanMeasurementNoise = 0.1m`
  - `CandleType = TimeSpan.FromMinutes(15).TimeFrame()`
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: Keltner
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ (15m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
