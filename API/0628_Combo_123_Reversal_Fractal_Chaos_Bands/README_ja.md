# コンボ戦略 123 Reversal & Fractal Chaos Bands
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

123 リバーサルパターンと Fractal Chaos Bands のブレイクアウトを組み合わせた戦略。
強気の 123 リバーサルが形成され、価格が上部フラクタルバンドの上でクローズしたときにロングを建てます。
弱気の 123 リバーサルが下部フラクタルバンドの下でのクローズと一致したときにショートを建てます。

## 詳細

- **エントリー条件**:
  - ロング: Reversal123 のロングシグナルかつ上部フラクタルバンドより高いクローズ。
  - ショート: Reversal123 のショートシグナルかつ下部フラクタルバンドより低いクローズ。
- **ロング/ショート**: 両方
- **エグジット条件**: 逆シグナル
- **ストップ**: いいえ
- **デフォルト値**:
  - `Length` = 15
  - `KSmoothing` = 1
  - `DLength` = 3
  - `Level` = 50m
  - `Pattern` = 1
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: パターンとブレイクアウト
  - 方向: 両方
  - インジケーター: Stochastic Oscillator, Fractal Chaos Bands
  - ストップ: いいえ
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
