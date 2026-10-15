# 損切り・利益確定（金額指定）戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

短期SMAが長期SMAを上抜けたときにロングエントリーし、逆のクロスでショートエントリーします。利益または損失が事前に定義した金額に達したときにポジションを閉じます。

## 詳細

- **エントリー条件**: SMA(14) が SMA(28) をクロス
- **ロング/ショート**: 両方
- **エグジット条件**: 金額ベースの利益または損失が目標に達する
- **ストップ**: あり
- **デフォルト値**:
  - `FastLength` = 14
  - `SlowLength` = 28
  - `TakeProfitMoney` = 200
  - `StopLossMoney` = 100
