# 高次ピボット戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

3バーまたは5バーのピボット定義を使用して、第1、第2、第3次のピボット高値と安値を検出します。この戦略は分析専用であり、注文を発注しません。

## 詳細

- **エントリー条件**:
  - なし（分析のみ）。
- **エグジット条件**:
  - なし。
- **インジケーター**:
  - 3バーまたは5バーのピボット検出器。
- **ストップ**: なし。
- **デフォルト値**:
  - `CandleType` = 5m
  - `UseThreeBar` = true
  - `DisplayFirstOrder` = true
  - `DisplaySecondOrder` = true
  - `DisplayThirdOrder` = true
- **フィルター**:
  - 単一時間軸
  - インジケーター: ピボット検出器
  - ストップ: なし
  - 複雑さ: 低
