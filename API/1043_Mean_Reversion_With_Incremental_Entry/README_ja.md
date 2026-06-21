# 段階的エントリーによる平均回帰戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、価格が単純移動平均から定義されたパーセンテージ偏差したときに取引に入ります。価格が平均からさらに離れるにつれて、追加注文が段階的に発注されます。

価格が移動平均に戻るとポジションはクローズされます。

## 詳細

- **エントリー条件:**
  - **ロング:** `Low < SMA` かつ `Low` と `SMA` の差が `Initial Percent` 以上。
  - **ショート:** `High > SMA` かつ `High` と `SMA` の差が `Initial Percent` 以上。
- **段階的エントリー:** 前回エントリーからさらに `Percent Step` ごとに新規注文が追加される。
- **エグジット条件:**
  - **ロング:** `Close ≥ SMA`.
  - **ショート:** `Close ≤ SMA`.
- **インジケーター:** SMA.
- **デフォルト値:**
  - `MA Length` = 30.
  - `Initial Percent` = 5.
  - `Percent Step` = 1.
