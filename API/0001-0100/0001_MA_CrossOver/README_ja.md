# 移動平均クロス戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、短期と長期の指数移動平均線の関係を追跡します。上抜けではロングへ新規建てまたは反転し、下抜けではショートへ新規建てまたは反転します。シグナルは確定したローソク足だけで評価されます。

## 詳細

- **ロングエントリー**：短期 EMA が長期 EMA を下から上へ交差します。
- **ショートエントリー**：短期 EMA が長期 EMA を上から下へ交差します。
- **決済**：反対方向の交差でポジションを反転し、割合ベースのストップロスで早期決済する場合があります。
- **デフォルト値**：
  - `FastLength` = 100
  - `SlowLength` = 400
  - `StopLossPercent` = 2
  - `CandleType` = 1 分
- **実装**：C# と Python。
