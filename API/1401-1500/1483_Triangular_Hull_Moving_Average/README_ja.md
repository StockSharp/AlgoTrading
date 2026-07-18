# 三角Hull移動平均戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

2バー遅延を使ったHull移動平均のクロスに基づく戦略。

Hull移動平均を2本前のバーの値と比較します。上方クロスでロングポジション、下方クロスでショートポジションを開きます。方向はロングのみまたはショートのみに制限することができます。

## 詳細
- **エントリー条件**: 2バー遅延を伴うHMAクロス。
- **ロング/ショート**: 設定可能。
- **エグジット条件**: 逆シグナルまたは方向フィルター。
- **ストップ**: なし。
- **デフォルト値**:
  - `Length` = 40
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `EntryMode` = EntryDirection.LongAndShort
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 設定可能
  - インジケーター: MA
  - ストップ: なし
  - 複雑さ: 基本
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
