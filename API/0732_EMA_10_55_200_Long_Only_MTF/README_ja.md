# EMA 10/55/200 ロングのみ MTF 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、4時間足のEMAクロスオーバーが日足と週足の強気トレンドと一致したときにロングポジションを開きます。

## 詳細

- **エントリー条件**:
  - `EMA10` が `EMA55` を上抜けし、ローソク足の高値が `EMA55` を上回る場合、または `EMA55` が `EMA200` を上抜けする場合、または `EMA10` が `EMA500` を上抜けする場合。
  - 日足の `EMA55` が `EMA200` を上回り、週足の `EMA55` が `EMA200` を上回っている。
- **エグジット条件**:
  - `EMA10` が `EMA200` または `EMA500` を下抜けする。
  - 価格がストップロスレベルに達する。
- **パラメーター**:
  - `EMA 10 Length` = 10
  - `EMA 55 Length` = 55
  - `EMA 200 Length` = 200
  - `EMA 500 Length` = 500
  - `Stop Loss %` = 5
