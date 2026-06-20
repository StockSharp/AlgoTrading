# Arpeet MACD戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Arpeet MACD戦略は、ゼロラインフィルターを使用してMACDクロスオーバーを取引します。MACDラインがゼロ以下にある状態でシグナルラインを上抜けるとロングシグナルが発生します。MACDがゼロ以上にある状態でシグナルラインを下抜けるとショートシグナルが発生します。

## 詳細

- **エントリー条件**:
  - **ロング**: MACDがシグナルを上抜けかつMACD < 0。
  - **ショート**: MACDがシグナルを下抜けかつMACD > 0。
- **ストップ**: なし。
- **デフォルト値**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
- **フィルター**:
  - カテゴリ: インジケーター
  - 方向: 両方
  - インジケーター: MACD
  - ストップ: いいえ
  - 複雑さ: 低
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
