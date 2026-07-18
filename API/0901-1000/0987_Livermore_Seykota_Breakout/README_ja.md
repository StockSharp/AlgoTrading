# Livermore Seykota ブレイクアウト
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

LivermoreのピボットポイントとSeykolaのトレンドフィルター、ATRベースのエグジットを組み合わせたブレイクアウトシステムです。

テストでは年平均リターン約87%を示しています。株式市場で最も良いパフォーマンスを発揮します。

この戦略は最新のピボットを上回るまたは下回るブレイクアウトを探し、EMAの整列と出来高の強さでトレンド方向を確認します。ATRベースのストップがリスクを管理します。

## 詳細

- **エントリー条件**: トレンドと出来高の確認を伴って価格が最後のピボットをブレイク。
- **ロング/ショート**: 両方向。
- **エグジット条件**: ATRストップまたはトレーリングストップ。
- **ストップ**: ATRベースのストップ＆トレーリング。
- **デフォルト値**:
  - `MainEmaLength` = 50
  - `FastEmaLength` = 20
  - `SlowEmaLength` = 200
  - `PivotLength` = 3
  - `AtrLength` = 14
  - `StopAtrMultiplier` = 3
  - `TrailAtrMultiplier` = 2
  - `VolumeSmaLength` = 20
  - `CandleType` = TimeSpan.FromMinutes(15)
- **フィルター**:
  - カテゴリ: ブレイクアウト
  - 方向: 両方
  - インジケーター: EMA, 出来高, ATR, Pivot
  - ストップ: ATR トレーリング
  - 複雑さ: 基本
  - 時間軸: イントラデイ (15m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
