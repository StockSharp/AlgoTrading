# 1日あたりの取引をフィルタリングするテーブル戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

SMA50とSMA200を使用した固定の利益と損失の目標を持つシンプルな移動平均クロスオーバー戦略。

## 詳細

- **エントリー**
  - ロング: SMA50がSMA200を上向きにクロス。
  - ショート: SMA50がSMA200を下向きにクロス。
- **エグジット**: 目標またはストップに達したときにポジションをクローズ。
