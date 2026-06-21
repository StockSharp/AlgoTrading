# 流動性エングルフメント戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、価格が最近の流動性の高値または安値に触れた後に発生する強気・弱気のエングルフィングパターンを検出します。取引はモードでフィルタリングされ、固定のストップロスとpips単位のオプションのテイクプロフィットを含みます。

## 詳細

- **エントリー条件**:
  - **ロング**: 下方流動性タッチ後の強気エングルフィング。
  - **ショート**: 上方流動性タッチ後の弱気エングルフィング。
- **エグジット条件**: 反対シグナル、ストップロスまたはテイクプロフィット。
- **ロング/ショート**: 設定可能（デフォルトは両方）。
- **インジケーター**: Highest, Lowest。
- **ストップ**: `StopLossPips` およびオプションの `TakeProfitPips`。
- **デフォルト値**:
  - `CandleType` = 1分
  - `UpperLookback` = 10
  - `LowerLookback` = 10
  - `StopLossPips` = 10
  - `TakeProfitPips` = 20
  - `Mode` = Both
