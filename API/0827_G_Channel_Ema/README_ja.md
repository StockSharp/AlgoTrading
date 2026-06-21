# G-Channel と EMA 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

G-ChannelのチャネルロジックとトレンドフィルターとしてのEMAを組み合わせた戦略です。

最後のクロスが下方向かつ価格がEMAを下回っているときに買い。最後のクロスが上方向かつ価格がEMAを上回っているときに売り。

## 詳細

- **エントリー条件**: EMAフィルター付きのG-Channel状態。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 反対シグナル。
- **ストップ**: いいえ。
- **デフォルト値**:
  - `ChannelLength` = 100
  - `EmaLength` = 200
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: G-Channel, EMA
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
