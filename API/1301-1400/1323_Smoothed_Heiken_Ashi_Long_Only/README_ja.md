# 平滑化 Heiken Ashi ロングのみ戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

平滑化されたHeikin-Ashiローソク足を使用したロングのみの戦略。平滑化されたローソク足が赤から緑に変わったときに買い、赤に戻ったときに決済します。

## 詳細

- **エントリー条件**: 平滑化されたHAが赤から緑に変わる
- **ロング/ショート**: ロングのみ
- **エグジット条件**: 平滑化されたHAが赤になる
- **ストップ**: なし
- **デフォルト値**:
  - `EmaLength` = 10
  - `SmoothingLength` = 10
