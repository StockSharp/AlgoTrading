# レンジフィルター ATR TP/SL戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

価格がレンジフィルターのバンドを交差したときにエントリーし、ATRベースの利確・損切りレベルでイグジットする戦略です。

## 詳細

- **エントリー条件**: 価格が上バンドを上抜けでロング、下バンドを下抜けでショート。
- **ロング/ショート**: 両方。
- **エグジット条件**: ATRベースの利確または損切り。
- **ストップ**: ATRベース、トレード開始時に固定。
- **デフォルト値**:
  - `RangeFilterLength` = 20
  - `RangeFilterMultiplier` = 1.5
  - `AtrLength` = 14
  - `TakeProfitMultiplier` = 1.5
  - `StopLossMultiplier` = 1.5
- **フィルター**: なし。
- **複雑さ**: 中程度。
- **時間軸**: 設定可能。
