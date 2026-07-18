# Liquidex 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

価格がケルトナーチャネルのバンドの外に出たときにエントリーし、ストップロス、テイクプロフィット、ブレイクイーブン、トレーリングストップでリスクを管理するブレイクアウト戦略です。

## 詳細

- **エントリー条件**:
  - ロング: 終値がケルトナーチャネルの上限バンドを上抜け。
  - ショート: 終値がケルトナーチャネルの下限バンドを下抜け。
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - ストップロスまたはテイクプロフィットのレベルに到達。
  - 利益目標達成後にストップをブレイクイーブンに移動。
  - トレーリングストップが起動。
- **ストップ**: はい。
- **デフォルト値**:
  - `KcPeriod` = 10
  - `UseKcFilter` = true
  - `StopLoss` = 30
  - `TakeProfit` = 0
  - `MoveToBe` = 15
  - `MoveToBeOffset` = 2
  - `TrailingDistance` = 5
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **フィルター**:
  - カテゴリ: チャネル
  - 方向: 両方
  - インジケーター: Keltner
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
