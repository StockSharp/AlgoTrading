# Dskyz (DAFE) GENESIS 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Dskyz (DAFE) GENESIS戦略の簡略版です。短期モメンタムがトレンドフィルターとRSIと一致した時にシステムが取引します。

## 詳細

- **エントリー条件**:
  - **ロング**: `SMA(9) > SMA(30)` かつ `RSI > 55` かつ `EMA(8) > EMA(21)`。
  - **ショート**: `SMA(9) < SMA(30)` かつ `RSI < 45` かつ `EMA(8) < EMA(21)`。
- **ロング/ショート**: 両方向。
- **エグジット条件**:
  - **ロング**: `EMA(8) < EMA(21)`。
  - **ショート**: `EMA(8) > EMA(21)`。
- **ストップ**: なし。
- **デフォルト値**:
  - `RSI Length` = 9。
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: RSI, EMA, SMA
  - ストップ: いいえ
  - 複雑さ: シンプル
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
