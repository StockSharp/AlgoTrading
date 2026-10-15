# Cm Manual Grid — 手動グリッド戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Cm Manual Grid は、現在の価格周辺に設定可能なストップ注文とリミット注文のグリッドを配置します。各新規注文はボリュームを固定の増分だけ増加させます。戦略は利益目標に達したときにロングまたはショートポジションを個別にクローズでき、トレーリング利益メカニズムを含みます。

## 詳細

- **タイプ**: 保留注文によるグリッドトレーディング
- **注文**: Buy Stop、Sell Stop、Buy Limit、Sell Limit
- **ボリューム**: 初期ボリューム `Lot`、増分 `LotPlus`
- **利益管理**:
  - `CloseProfitB` はロングポジションをクローズ
  - `CloseProfitS` はショートポジションをクローズ
  - `ProfitClose` はすべてのポジションをクローズ
  - `TralStart` と `TralClose` はトレーリング利益を管理
- **デフォルト値**:
  - `OrdersBuyStop` = 5
  - `OrdersSellStop` = 5
  - `OrdersBuyLimit` = 5
  - `OrdersSellLimit` = 5
  - `FirstLevel` = 5 ステップ
  - `StepBuyStop` = 10
  - `StepSellStop` = 10
  - `StepBuyLimit` = 10
  - `StepSellLimit` = 10
  - `Lot` = 0.1
  - `LotPlus` = 0.1
  - `CloseProfitB` = 10
  - `CloseProfitS` = 10
  - `ProfitClose` = 10
  - `TralStart` = 10
  - `TralClose` = 5
