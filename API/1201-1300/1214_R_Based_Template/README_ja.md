# Rベース戦略テンプレート
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

リスク管理されたポジションサイジングと設定可能なストップタイプを持つRSIベースの戦略。

## 詳細

- **エントリー条件**:
  - RSIが`OversoldLevel`を下回ったときロング。
  - RSIが`OverboughtLevel`を上回ったときショート。
- **ロング/ショート**: 両方。
- **エグジット条件**: `TpRValue`の倍数を使ったストップロスまたはテイクプロフィット。
- **ストップ**:
  - Fixed、Atr、Percentage または Ticks。
- **デフォルト値**:
  - `RiskPerTradePercent` = 1
  - `RsiLength` = 14
  - `OversoldLevel` = 30
  - `OverboughtLevel` = 70
  - `StopLossType` = Fixed
  - `SlValue` = 100
  - `AtrLength` = 14
  - `AtrMultiplier` = 2
  - `TpRValue` = 2
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: オシレーター
  - 方向: 両方
  - インジケーター: RSI、ATR
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: Variable
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
