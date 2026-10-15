# MACDモメンタムリバーサル
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はMACDヒストグラムを使ってモメンタムの逆転を検出します。
強気ローソク足が拡大してもMACDヒストグラムが低下した場合にショート。
弱気ローソク足が拡大してもMACDヒストグラムが上昇した場合に買い。

## 詳細

- **エントリー条件**: ローソク足の実体が大きくMACDモメンタムが弱まっている。
- **ロング/ショート**: 両方。
- **エグジット条件**: 逆シグナル。
- **ストップ**: いいえ。
- **デフォルト値**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: モメンタム
  - 方向: 両方
  - インジケーター: MACD
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: イントラデイ (1m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
