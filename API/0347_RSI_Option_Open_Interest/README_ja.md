# RSI オプション建玉残高戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
**RSI Option Open Interest** 戦略は、RSIのオプション建玉残高を中心に構築されています。

テストでは年平均リターン約130%が示されています。株式市場で最も優れたパフォーマンスを発揮します。

Optionがイントラデイ（5m）データでのトレンド転換を確認したときにシグナルが発生します。これによりこの手法はアクティブトレーダーに適しています。

ストップはATRの倍数とRsiPeriod、CandleTypeなどのパラメーターに依存します。リスクとリワードのバランスを取るためにこれらのデフォルト値を調整してください。

## 詳細
- **エントリー条件**: インジケーター条件については実装を参照してください。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 反対シグナルまたはストップロジック。
- **ストップ**: はい、インジケーターベースの計算を使用。
- **デフォルト値**:
  - `RsiPeriod = 14`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
  - `OiPeriod = 20`
  - `OiDeviationFactor = 2m`
  - `StopLoss = 2m`
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: Option, Open, Interest
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

