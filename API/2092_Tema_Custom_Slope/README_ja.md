# TEMA カスタム傾斜戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Triple Exponential Moving Average (TEMA)の傾斜変化を使用したリバーサル戦略です。指定された時間軸でインジケーターを計算し、方向転換に反応します。

## 動作方法

- **エントリー条件**:
  - **ロング**: TEMAが下落から上昇に転じる。
  - **ショート**: TEMAが上昇から下落に転じる。
- **エグジット条件**: 逆シグナルにより既存のポジションをクローズ。
- **インジケーター**: Triple Exponential Moving Average。

## 主なパラメーター

- `TemaLength` – TEMA計算のバー数。
- `CandleType` – 分析に使用するローソク足の時間軸。
