# 組み込みKelly比率戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

移動平均とATRバンドを使ったチャネルブレイクアウト戦略で、Kelly比率に基づくポジションサイジングを採用しています。

## 詳細

- **エントリー条件**: 価格がATRベースのバンドを上抜けまたは下抜け。
- **ロング/ショート**: 両方。
- **エグジット条件**: オプションのテイクプロフィットとストップロス。
- **ストップ**: オプション。
- **デフォルト値**:
  - `Length` = 20
  - `Multiplier` = 1
  - `AtrLength` = 10
  - `UseEma` = true
  - `UseKelly` = true
  - `UseTakeProfit` = false
  - `UseStopLoss` = false
  - `TakeProfit` = 10
  - `StopLoss` = 1
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: ブレイクアウト
  - 方向: 両方
  - インジケーター: MA, ATR
  - ストップ: オプション
  - 複雑さ: 基本
  - 時間軸: イントラデイ (1m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
