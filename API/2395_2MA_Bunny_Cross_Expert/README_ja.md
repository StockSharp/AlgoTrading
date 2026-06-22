# 2MA Bunny Cross Expert
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

**2MA Bunny Cross Expert**戦略は2本の単純移動平均のクロスオーバーを取引します。速い平均が遅い平均を上に抜けたときにロング取引を建て、速い平均が遅い平均を下に抜けたときにショート取引を建てます。新しいポジションを建てる前に反対側のポジションをクローズします。

## 詳細

- **目的**: 移動平均クロスオーバーによるトレンドフォロー
- **取引**: ロングおよびショート
- **インジケーター**: 速い単純移動平均と遅い単純移動平均
- **ストップ**: なし
- **デフォルト値**:
  - `CandleType` = 1 minute
  - `FastLength` = 5
  - `SlowLength` = 20
