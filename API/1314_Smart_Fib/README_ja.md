# Smart Fib戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

単純移動平均のブレイクアウトをエントリーに、ATRベースのフィボナッチバンドをエグジットに使用する戦略です。

## 詳細

- **エントリー条件**: 終値がSMAを上または下にクロスすること。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 価格がATRフィボナッチバンドに到達すること。
- **ストップ**: なし。
- **デフォルト値**:
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `SmaLength` = 50
  - `FibSmaLength` = 8
  - `AtrLength` = 6
  - `FirstFactor` = 1.618
  - `SecondFactor` = 2.618
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: SMA, ATR
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
