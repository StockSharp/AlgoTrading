# `security()` 再考
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

`security()` 再考は、StockSharpで上位時間軸のデータにアクセスし、前回値または現在値を使用してリペイントを制御する方法を示します。

## 詳細

- **目的**: リペイント制御付きの上位時間軸データアクセス
- **取引**: デモンストレーション
- **インジケーター**: なし
- **ストップ**: なし
- **デフォルト値**:
  - `CandleType` = 1 minute
  - `HigherTimeframe` = 5 minutes
  - `Repaint` = false
