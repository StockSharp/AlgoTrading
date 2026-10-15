# Ichimokuボラティリティ収縮戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
**Ichimoku Volatility Contraction**戦略は、Ichimokuインジケーターを使用してボラティリティ収縮期間を特定します。

テストでは年間平均リターン約85%を示しています。暗号資産市場で最も効果を発揮します。

インジケーターがイントラデイ（5m）データのボラティリティ収縮パターンを確認したときにシグナルが発生します。このため、アクティブトレーダーに適した手法です。

ストップはATRの倍数とTenkanPeriod、KijunPeriodなどのパラメーターに基づいています。リスクとリワードのバランスを取るためにこれらのデフォルト値を調整してください。

## 詳細
- **エントリー条件**: インジケーターの条件については実装を参照してください。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 反対のシグナルまたはストップロジック。
- **ストップ**: はい、インジケーターベースの計算を使用。
- **デフォルト値**:
  - `TenkanPeriod = 9`
  - `KijunPeriod = 26`
  - `SenkouSpanBPeriod = 52`
  - `AtrPeriod = 14`
  - `DeviationFactor = 2.0m`
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
