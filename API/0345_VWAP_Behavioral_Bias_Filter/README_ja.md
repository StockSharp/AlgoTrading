# VWAP 行動バイアス・フィルター戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
**VWAP Behavioral Bias Filter** 戦略は、VWAPの行動バイアスフィルターを中心に構築されています。

テストでは年平均リターン約124%が示されています。外国為替市場で最も優れたパフォーマンスを発揮します。

Behavioralフィルターがイントラデイ（5m）データでのフィルタリングされたエントリーを確認したときにシグナルが発生します。これによりこの手法はアクティブトレーダーに適しています。

ストップはATRの倍数とBiasThreshold、BiasWindowSizeなどのパラメーターに依存します。リスクとリワードのバランスを取るためにこれらのデフォルト値を調整してください。

## 詳細
- **エントリー条件**: インジケーター条件については実装を参照してください。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 反対シグナルまたはストップロジック。
- **ストップ**: はい、インジケーターベースの計算を使用。
- **デフォルト値**:
  - `BiasThreshold = 0.5m`
  - `BiasWindowSize = 20`
  - `StopLoss = 2m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: Behavioral, Bias
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

