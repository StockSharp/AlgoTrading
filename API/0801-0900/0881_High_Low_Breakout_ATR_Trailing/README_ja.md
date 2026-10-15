# 高値安値ブレイクアウトATRトレーリングストップ戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はセッション最初の30分間のレンジのブレイクアウトを取引します。価格が初期の高値または安値を越えると、ATRベースのトレーリングストップを伴うポジションが開かれます。すべてのポジションは指定されたイントラデイ時刻に決済されます。

## 詳細
- **エントリー条件**:
  - **ロング**: 終値が最初の30分の高値を上抜け
  - **ショート**: 終値が最初の30分の安値を下抜け
- **ロング/ショート**: 設定可能（`Direction`）。
- **エグジット条件**:
  - ATRトレーリングストップまたは対称ターゲット
  - `ExitHour:ExitMinute`にすべてのポジションを決済
- **ストップ**: あり、ATRベース。
- **デフォルト値**:
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 3.5m
  - `RiskPerTrade` = 2m
  - `AccountSize` = 10000m
  - `SessionStartHour` = 9
  - `SessionStartMinute` = 15
  - `ExitHour` = 15
  - `ExitMinute` = 15
  - `CandleType` = TimeSpan.FromMinutes(30)
- **フィルター**:
  - カテゴリ: ブレイクアウト
  - 方向: 設定可能
  - インジケーター: ATR
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
