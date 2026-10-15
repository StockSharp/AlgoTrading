# IBS内部バー強度戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は内部バー強度（IBS）が下限閾値を下回ったときにロングエントリーし、指定された時間ウィンドウ内でIBSが上限閾値を上回ったときにエグジットします。

## 詳細

- **エントリー条件**:
  - IBS < `LowerThreshold`。
  - `StartTime`と`EndTime`の間の時間。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**:
  - IBS >= `UpperThreshold`。
- **ストップ**: なし。
- **デフォルト値**:
  - `UpperThreshold` = 0.8
  - `LowerThreshold` = 0.2
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: ロングのみ
  - インジケーター: なし
  - ストップ: いいえ
  - 複雑さ: 低
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 低
