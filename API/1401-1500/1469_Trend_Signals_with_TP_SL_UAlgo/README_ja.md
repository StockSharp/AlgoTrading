# TP・SL付きトレンドシグナル戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は ATR ベースのチャネルを使用してトレンドの方向を判断します。価格が上部バンドを上抜けると新たな上昇トレンドが始まり、ロングエントリーが発生します。価格が下部バンドを下抜けると下降トレンドが始まり、ショートエントリーが発生します。各トレードには ATR 乗数を使ったストップロスとテイクプロフィットを設定します。

## 詳細

- **エントリー条件**:
  - **ロング**: トレンドが上向きに転換する。
  - **ショート**: トレンドが下向きに転換する。
- **エグジット**: `entry ∓ ATR * SL` でストップロス、`entry ± ATR * TP` でテイクプロフィット。
- **ストップ**: はい、ストップロスとテイクプロフィットの両方。
- **デフォルト値**:
  - `Sensitivity` = 2
  - `ATR Length` = 14
  - `ATR TP Multiplier` = 2
  - `ATR SL Multiplier` = 1
