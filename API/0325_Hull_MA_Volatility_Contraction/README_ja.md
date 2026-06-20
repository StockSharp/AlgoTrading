# Hull MA Volatility Contraction戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

**Hull MA Volatility Contraction**戦略はボラティリティ収縮フィルターを備えたHull Moving Averageを中心に構築されています。

テストでは年平均リターン約76%が示されています。外国為替市場で最もよいパフォーマンスを発揮します。

インジケーターがイントラデイデータでのボラティリティ収縮パターンを確認するとシグナルが発生します (15m)。この手法はアクティブなトレーダーに適しています。

ストップはATRの倍数とHmaPeriod、AtrPeriodなどのパラメーターに依存します。リスクとリワードのバランスを取るためにこれらのデフォルト値を調整してください。

## 詳細
- **エントリー条件**: インジケーター条件の実装を参照。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 逆シグナルまたはストップロジック。
- **ストップ**: はい、インジケーターベースの計算を使用。
- **デフォルト値**:
  - `HmaPeriod = 9`
  - `AtrPeriod = 14`
  - `VolatilityContractionFactor = 2.0m`
  - `CandleType = TimeSpan.FromMinutes(15).TimeFrame()`
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: 複数のインジケーター
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ (15m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
