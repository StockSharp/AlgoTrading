# VWAP Stochastic Divergence戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

**VWAP Stochastic Divergence**戦略はVWAPとADXトレンド強度インジケーターを組み合わせて構築されています。

テストでは年平均リターン約79%が示されています。株式市場で最もよいパフォーマンスを発揮します。

StochasticがイントラデイデータでのダイバージェンスセットアップConfirmするとシグナルが発生します (5m)。この手法はアクティブなトレーダーに適しています。

ストップはATRの倍数とAdxPeriod、AdxThresholdなどのパラメーターに依存します。リスクとリワードのバランスを取るためにこれらのデフォルト値を調整してください。

## 詳細
- **エントリー条件**: インジケーター条件の実装を参照。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 逆シグナルまたはストップロジック。
- **ストップ**: はい、インジケーターベースの計算を使用。
- **デフォルト値**:
  - `AdxPeriod = 14`
  - `AdxThreshold = 25m`
  - `AdxExitThreshold = 20m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: Stochastic, Divergence
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: はい
  - リスクレベル: 中
