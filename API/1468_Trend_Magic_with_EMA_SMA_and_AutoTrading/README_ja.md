# Trend Magic と EMA、SMA、自動取引戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は CCI ベースの Trend Magic ラインを EMA(45)、SMA(90)、SMA(180) フィルターとともに使用します。移動平均が強気に並ぶ中で Trend Magic が青に変わるとロングトレードが開かれます。ラインが赤になり移動平均が弱気に並ぶとショートトレードが発生します。各ポジションには SMA90 にストップを置き、固定のリスク・リワード比に基づいてテイクプロフィットを設定します。

## 詳細

- **エントリー条件**:
  - **ロング**: `EMA45 > SMA90 > SMA180` かつ Trend Magic が青になる。
  - **ショート**: `EMA45 < SMA90 < SMA180` かつ Trend Magic が赤になる。
- **エグジット**: エントリー時に SMA90 でストップロスを設定し、`entry ± risk * ratio` でテイクプロフィット。
- **ストップ**: ストップロスとテイクプロフィットの両方。
- **デフォルト値**:
  - `CCI Period` = 21
  - `ATR Period` = 7
  - `ATR Multiplier` = 1.0
  - `Risk Reward` = 1.5
