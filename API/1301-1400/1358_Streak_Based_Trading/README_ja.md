# 連続ストリーク取引戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

連続した勝ちローソク足と負けローソク足を追跡します。指定されたストリーク数に達した後、戦略は逆方向にエントリーし、固定本数のローソク足の間ポジションを保持します。胴体サイズに基づいてドージローソク足は無視されます。

## 詳細

- **エントリー条件**: 勝ち/負けのストリーク達成後に逆方向。
- **ロング/ショート**: 設定可能 (`TradeDirection`)。
- **エグジット条件**: `HoldDuration` 本のローソク足後。
- **ストップ**: いいえ。
- **デフォルト値**:
  - `TradeDirection` = Long
  - `StreakThreshold` = 8
  - `HoldDuration` = 7
  - `DojiThreshold` = 0.01
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: リバーサル
  - 方向: 設定可能
  - インジケーター: Price Action
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
