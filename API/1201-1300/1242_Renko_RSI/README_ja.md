# Renko RSI 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

RSIの買われすぎ/売られすぎシグナルを使用してRenkoブロックを取引する戦略。

テストでは中程度のパフォーマンスを示し、明確なRenkoトレンドがある市場で最も機能します。

Renko RSIはATRから構築されたRenkoブロックを使用し、短期RSIを適用します。RSIが売られすぎレベルを上抜けると買いシグナル、買われすぎレベルを下抜けると売りシグナルが発生します。

## 詳細

- **エントリー条件**: RSIが売られすぎまたは買われすぎレベルをクロス。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 逆シグナル。
- **ストップ**: なし。
- **デフォルト値**:
  - `RenkoAtrLength` = 14
  - `RsiLength` = 2
  - `RsiOverbought` = 80
  - `RsiOversold` = 20
  - `CandleType` = Renko ATR(14)
- **フィルター**:
  - カテゴリ: モメンタム
  - 方向: 両方
  - インジケーター: RSI, Renko
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: Renko
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
