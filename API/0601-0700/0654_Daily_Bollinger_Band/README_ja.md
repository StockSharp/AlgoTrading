# 日足 Bollinger Band 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

トレンドフィルターとATRベースのポジションサイジングを使用して、日足のBollinger Bandブレイクアウトを取引する戦略。

## 詳細

- **エントリー条件**: ロングは価格が上部バンドを傾きが正の状態でクロス、ショートは価格が下部バンドを傾きが負の状態でクロス。
- **エグジット条件**: 価格が中間バンドをクロスした時にポジションをクローズ。
