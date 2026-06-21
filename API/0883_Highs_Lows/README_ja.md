# 高値安値戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

最高値と最低値のレンジに対するローソク足の中値に基づいて取引する戦略です。

現在のローソク足の中値が最高値と最低値の平均を下回り、正規化距離がLowThresholdを下回る場合に買いを入れます。中値が平均を上回り、正規化距離がHighThresholdを上回った場合にロングポジションを決済します。

## 詳細

- **エントリー条件**: 中値が平均を下回り、正規化距離がLowThresholdを下回る。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**: 中値が平均を上回り、正規化距離がHighThresholdを上回る。
- **ストップ**: なし。
- **デフォルト値**:
  - `Range` = 100
  - `LowThreshold` = 15m
  - `HighThreshold` = 85m
  - `CandleType` = TimeSpan.FromMinutes(240)
- **フィルター**:
  - カテゴリ: レンジ
  - 方向: ロング
  - インジケーター: Highest, Lowest
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: イントラデイ (240m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
