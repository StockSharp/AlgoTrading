# Donky MA TP SL 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、2つのテイクプロフィット目標とストップロスを伴う移動平均クロスオーバーをトレードします。速いSMAが遅いSMAを上抜けするとロング、下抜けするとショートに入ります。ポジションの半分を最初の目標で決済し、残りを2番目の目標またはストップロスで決済します。

## 詳細

- **エントリー条件**:
  - **ロング**: 速いSMAが遅いSMAを上抜け。
  - **ショート**: 速いSMAが遅いSMAを下抜け。
- **ロング/ショート**: 両方。
- **エグジット条件**: 2つの固定テイクプロフィットレベルまたは固定ストップロス。
- **ストップ**: はい。
- **デフォルト値**:
  - `FastLength` = 10
  - `SlowLength` = 30
  - `TakeProfit1Pct` = 0.03m
  - `TakeProfit2Pct` = 0.06m
  - `StopLossPct` = 0.01m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: SMA
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 低
