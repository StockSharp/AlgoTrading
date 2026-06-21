# 月次ブレイクアウト戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

選択した暦月のみ、当月の高値または安値のブレイクアウトを取引します。方向は`EntryOption`で選択し、ポジションは一定本数の足の後に決済されます。

## 詳細

- **エントリー条件**:
  - `EntryOption`と選択された月に依存（例：終値が月間高値を上回ったときにロング）。
- **ロング/ショート**: 設定可能。
- **エグジット条件**: `HoldingPeriod`本後に決済。
- **ストップ**: いいえ。
- **デフォルト値**:
  - `EntryOption` = LongAtHigh
  - `HoldingPeriod` = 5
  - `CandleType` = TimeSpan.FromDays(1)
- **フィルター**:
  - カテゴリ: ブレイクアウト
  - 方向: 設定可能
  - インジケーター: なし
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: 日足
  - 季節性: はい
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
