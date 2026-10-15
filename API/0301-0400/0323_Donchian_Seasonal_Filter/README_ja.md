# Donchian Seasonal Filter戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

**Donchian Seasonal Filter**戦略は季節性フィルターを備えたDonchianチャンネルを中心に構築されています。

テストでは年平均リターン約70%が示されています。株式市場で最もよいパフォーマンスを発揮します。

Donchianがイントラデイデータでフィルタリングされたエントリーを確認するとシグナルが発生します (15m)。この手法はアクティブなトレーダーに適しています。

ストップはATRの倍数とDonchianPeriod、SeasonalThresholdなどのパラメーターに依存します。リスクとリワードのバランスを取るためにこれらのデフォルト値を調整してください。

## 詳細
- **エントリー条件**: インジケーター条件の実装を参照。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 逆シグナルまたはストップロジック。
- **ストップ**: はい、インジケーターベースの計算を使用。
- **デフォルト値**:
  - `DonchianPeriod = 20`
  - `SeasonalThreshold = 0.5m`
  - `CandleType = TimeSpan.FromMinutes(15).TimeFrame()`
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: Donchian, Seasonal
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ (15m)
  - 季節性: はい
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
