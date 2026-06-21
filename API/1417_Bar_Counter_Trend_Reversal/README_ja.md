# バー逆張りリバーサル戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

連続して上昇または下落するバーを探し、価格がチャネルの極端に達したときに逆張りトレードを行います。

## 詳細

- **エントリー条件**: 連続した上昇または下落の系列（オプションの出来高・チャネル確認付き）
- **ロング/ショート**: 両方
- **エグジット条件**: 反対シグナル
- **ストップ**: いいえ
- **デフォルト値**:
  - `NoOfRises` = 3
  - `NoOfFalls` = 3
  - `ChannelLength` = 20
  - `ChannelMultiplier` = 2
- **フィルター**:
  - カテゴリ: リバーサル
  - 方向: 両方
  - インジケーター: Keltner Channel または Bollinger Bands
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
