# Smart Money ピボット戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はピボット高値・安値のブレイクアウトを取引します。価格が直近のピボット高値を上抜けたときにロングポジションを建て、直近のピボット安値を下抜けたときにショートポジションを建てます。各トレードには独自のストップロスとテイクプロフィットのパーセンテージが設定されます。

## 詳細

- **エントリー条件**: ピボット高値の上抜けまたはピボット安値の下抜け。
- **ロング/ショート**: 両方向。
- **エグジット条件**: ストップロスまたはテイクプロフィット。
- **ストップ**: はい。
- **デフォルト値**:
  - `EnableLongStrategy` = true
  - `LongStopLossPercent` = 1m
  - `LongTakeProfitPercent` = 1.5m
  - `EnableShortStrategy` = true
  - `ShortStopLossPercent` = 1m
  - `ShortTakeProfitPercent` = 1.5m
  - `Period` = 20
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: ブレイクアウト
  - 方向: 両方
  - インジケーター: Price Action
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ (1m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
