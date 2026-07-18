# Crypto戦略 SUSDT 10分
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

シンプルなEMAクロス戦略で、価格がEMAを上抜けて終値がEMAより高い場合にロング、逆の条件でショートに入る。ストップロスとテイクプロフィットはエントリー価格からのパーセンテージで定義される。

## 詳細

- **エントリー条件**:
  - **ロング**: `close > EMA` かつ `open < EMA`
  - **ショート**: `close < EMA` かつ `open > EMA`
- **ロング/ショート**: 両方。
- **エグジット条件**: テイクプロフィットまたはストップロス。
- **ストップ**: あり、テイクプロフィットとストップロスの両方。
- **デフォルト値**:
  - `CandleType` = 10分
  - `EmaLength` = 24
  - `TakeProfitPercent` = 4
  - `StopLossPercent` = 2
  - `OrderPercent` = 30
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: EMA
  - ストップ: あり
  - 複雑さ: 低
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
