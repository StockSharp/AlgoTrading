# 弱いバーを伴う新イントラデイ高値戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

ローソク足が安値付近で引けた`HighestLength`バーの新高値でロングエントリーします。価格が前バーの高値を上抜けてクローズしたときに退出します。

## 詳細

- **エントリー条件**:
  - ポジションなし、高値が直近`HighestLength`バーの最高値と等しく、かつ`(close - low)/(high - low) < WeakRatio`。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**: 前バーの高値を上抜けてクローズ。
- **ストップ**: なし。
- **デフォルト値**:
  - `HighestLength` = 10
  - `WeakRatio` = 0.15
  - `CandleType` = TimeSpan.FromMinutes(15)
- **フィルター**:
  - カテゴリ: ブレイクアウト
  - 方向: ロングのみ
  - インジケーター: Highest
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
