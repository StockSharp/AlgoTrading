# Bollinger Kalman Filter戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

**Bollinger Kalman Filter**戦略はBollinger Kalman Filterを中心に構築されています。

BollingerがイントラデイデータでフィルタリングされたエントリーConfirmするとシグナルが発生します (5m)。この手法はアクティブなトレーダーに適しています。

ストップはATRの倍数とBollingerLength、BollingerDeviationなどのパラメーターに依存します。リスクとリワードのバランスを取るためにこれらのデフォルト値を調整してください。

## 詳細
- **エントリー条件**: インジケーター条件の実装を参照。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 逆シグナルまたはストップロジック。
- **ストップ**: はい、インジケーターベースの計算を使用。
- **デフォルト値**:
  - `BollingerLength = 20`
  - `BollingerDeviation = 2.0m`
  - `KalmanQ = 0.01m`
  - `KalmanR = 0.1m`
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
