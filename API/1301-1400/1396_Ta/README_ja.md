# Ta戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

サポート・レジスタンスのピボット、RSI、ADX確認を伴うMACDクロスオーバーに基づく戦略。一部エグジットを伴う2つの利益目標を使用します。

## 詳細

- **エントリー**
  - **ロング**: MACDがシグナルを上向きにクロス、価格がレジスタンス上、RSI > 50、+DI > -DI、ADX > 20。
  - **ショート**: MACDがシグナルを下向きにクロス、価格がサポート下、RSI < 50、-DI > +DI、ADX > 20。
- **エグジット**: 2つの目標利益レベルとストップロス。
