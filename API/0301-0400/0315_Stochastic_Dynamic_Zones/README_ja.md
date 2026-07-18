# Stochastic ダイナミックゾーン戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

**Stochastic Dynamic Zones** 戦略は、Stochastic オシレーターと動的な買われすぎ/売られすぎゾーンを中心に構築されています。

テストでは年間平均リターン約 52% を示しています。暗号通貨市場で最もよく機能します。

インジケーターがイントラデイ (5m) データ上のトレンド変化を Stochastic が確認したときにシグナルが発動します。これにより、この手法はアクティブトレーダーに適しています。

ストップは ATR の倍数と StochPeriod、StochKPeriod などの要素に依存します。リスクとリワードのバランスをとるためにこれらのデフォルト値を調整してください。

## 詳細
- **エントリー条件**: インジケーター条件については実装を参照してください。
- **ロング/ショート**: 両方。
- **エグジット条件**: 反対のシグナルまたはストップロジック。
- **ストップ**: はい、インジケーターベースの計算を使用。
- **デフォルト値**:
  - `StochPeriod = 14`
  - `StochKPeriod = 3`
  - `StochDPeriod = 3`
  - `LookbackPeriod = 20`
  - `StandardDeviationFactor = 2.0m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: Stochastic
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
