# MartinGaleスキャルピング戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

SMA(3)がSMA(8)をクロスするとエントリーが発動し、マルチンゲール方式のピラミッディングを行います。ストップまたはテイクプロフィットに達するまで、各バーで追加注文が加えられます。

## 詳細

- **エントリー条件**: ロングでは`SMA3`が`SMA8`より上、ショートでは下；シグナルが継続する間、新規エントリーを追加。
- **ロング/ショート**: `TradingMode`で設定可能。
- **エグジット条件**: 価格が`TakeProfit`または`StopLoss`に達し、SMAの関係が逆転する。
- **ストップ**: あり、遅いSMAの値に基づく。
- **デフォルト値**:
  - `FastLength` = 3
  - `SlowLength` = 8
  - `TakeProfit` = 1.03
  - `StopLoss` = 0.95
  - `TradingMode` = Long
  - `CandleType` = 5 minutes
  - `MaxPyramids` = 5
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 設定可能
  - インジケーター: SMA
  - ストップ: はい
  - 複雑さ: 初心者
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 高
