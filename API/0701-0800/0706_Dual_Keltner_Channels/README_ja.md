# デュアル Keltner チャンネル戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

**デュアル Keltner チャンネル**戦略は、異なる乗数を持つ 2 つの Keltner チャンネルを使用してブレイクアウトを検出します。
価格が外側のバンドを突破し、内側のバンドに戻ったときに取引を開始します。
ストップとターゲットは固定パーセンテージで管理します。

## 詳細
- **エントリー条件**: 価格が外側の Keltner バンドを越え、同じ方向に内側のバンドを再び越える。
- **ロング/ショート**: 両方向。
- **エグジット条件**: ストップロス、テイクプロフィット、または逆シグナル。
- **ストップ**: あり、パーセンテージベース。
- **デフォルト値**:
  - `EmaPeriod = 50`
  - `InnerMultiplier = 2.75m`
  - `OuterMultiplier = 3.75m`
  - `MaxStopPercent = 10m`
  - `SlTpRatio = 1m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **フィルター**:
  - カテゴリ: ブレイクアウト
  - 方向: 両方
  - インジケーター: Keltner
  - ストップ: あり
  - 複雑さ: 中級
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
