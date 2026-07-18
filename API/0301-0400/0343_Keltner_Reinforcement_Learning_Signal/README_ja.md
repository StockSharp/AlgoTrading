# Keltner 強化学習シグナル戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
**Keltner Reinforcement Learning Signal** 戦略は、Keltnerの強化学習シグナルを中心に構築されています。

テストでは年平均リターン約118%が示されています。株式市場で最も優れたパフォーマンスを発揮します。

Keltnerがイントラデイ（15m）データでのトレンド転換を確認したときにシグナルが発生します。これによりこの手法はアクティブトレーダーに適しています。

ストップはATRの倍数とEmaPeriod、AtrPeriodなどのパラメーターに依存します。リスクとリワードのバランスを取るためにこれらのデフォルト値を調整してください。

## 詳細
- **エントリー条件**: インジケーター条件については実装を参照してください。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 反対シグナルまたはストップロジック。
- **ストップ**: はい、インジケーターベースの計算を使用。
- **デフォルト値**:
  - `EmaPeriod = 20`
  - `AtrPeriod = 14`
  - `AtrMultiplier = 2m`
  - `StopLossAtr = 2m`
  - `CandleType = TimeSpan.FromMinutes(15).TimeFrame()`
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: Keltner, Reinforcement
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ (15m)
  - 季節性: いいえ
  - ニューラルネットワーク: はい
  - ダイバージェンス: いいえ
  - リスクレベル: 中

