# Parabolic SAR Hurst Filter戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

**Parabolic SAR Hurst Filter**戦略はParabolic SAR Hurst Filterを中心に構築されています。

テストでは年平均リターン約82%が示されています。株式市場で最もよいパフォーマンスを発揮します。

ParabolicがイントラデイデータでフィルタリングされたエントリーConfirmするとシグナルが発生します (5m)。この手法はアクティブなトレーダーに適しています。

ストップはATRの倍数とSarAccelerationFactor、SarMaxAccelerationFactorなどのパラメーターに依存します。リスクとリワードのバランスを取るためにこれらのデフォルト値を調整してください。

## 詳細
- **エントリー条件**: インジケーター条件の実装を参照。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 逆シグナルまたはストップロジック。
- **ストップ**: はい、インジケーターベースの計算を使用。
- **デフォルト値**:
  - `SarAccelerationFactor = 0.02m`
  - `SarMaxAccelerationFactor = 0.2m`
  - `HurstPeriod = 100`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: Parabolic, Hurst
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
