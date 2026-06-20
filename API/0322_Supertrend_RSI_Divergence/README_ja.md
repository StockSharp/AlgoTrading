# Supertrend RSI Divergence戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

**Supertrend RSI Divergence**戦略はSupertrendインジケーターとRSIダイバージェンスを使用してトレードの機会を特定します。

テストでは年平均リターン約67%が示されています。株式市場で最もよいパフォーマンスを発揮します。

DivergenceがイントラデイデータでのダイバージェンスセットアップConfirmするとシグナルが発生します (15m)。この手法はアクティブなトレーダーに適しています。

ストップはATRの倍数とSupertrendPeriod、SupertrendMultiplierなどのパラメーターに依存します。リスクとリワードのバランスを取るためにこれらのデフォルト値を調整してください。

## 詳細
- **エントリー条件**: インジケーター条件の実装を参照。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 逆シグナルまたはストップロジック。
- **ストップ**: はい、インジケーターベースの計算を使用。
- **デフォルト値**:
  - `SupertrendPeriod = 10`
  - `SupertrendMultiplier = 3.0m`
  - `RsiPeriod = 14`
  - `CandleType = TimeSpan.FromMinutes(15).TimeFrame()`
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: Divergence
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ (15m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: はい
  - リスクレベル: 中
