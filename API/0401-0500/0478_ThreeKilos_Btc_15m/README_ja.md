# Three Kilos BTC 15m 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Three Kilos BTC 15m戦略は、3つのトリプル指数移動平均（TEMA）とSupertrendフィルターを組み合わせます。中間TEMAが短期TEMAを上抜け、低速TEMAより上にあり、Supetrendが上昇トレンドを示す場合にロングポジションを建てます。短期TEMAが中間TEMAを上抜け、低速TEMAより下にあり、Supetrendが下降トレンドを示す場合にショートポジションを建てます。固定パーセンテージのテイクプロフィットとストップロスでリスクを管理します。

## 詳細

- **エントリー条件**:
  - **ロング**: TEMA2がTEMA1を上抜け、TEMA2 > TEMA3、Supertrend上昇トレンド。
  - **ショート**: TEMA1がTEMA2を上抜け、TEMA2 < TEMA3、Supertrend下降トレンド。
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - テイクプロフィットまたはストップロス。
- **ストップ**: テイクプロフィット1%およびストップロス1%。
- **デフォルト値**:
  - `ShortPeriod` = 30
  - `LongPeriod` = 50
  - `Long2Period` = 140
  - `AtrLength` = 10
  - `Multiplier` = 2
  - `TakeProfit` = 1%
  - `StopLoss` = 1%
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: TEMA, Supertrend, ATR
  - ストップ: テイクプロフィットとストップロス
  - 複雑さ: 中程度
  - 時間軸: 15m
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
