# Stochastic RSI OHLC戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はStochastic RSIインジケーターからOHLCバーを構築し、モメンタムの転換で取引します。高値、安値、終値のRSIを計算し、各系列にStochasticオシレーターを適用します。Stochastic RSIがピボットから上昇してロングエントリーレベルを上抜けたときにロングポジションを開きます。ピボットから下落してショートエントリーレベルを下抜けたときにショートポジションを開きます。

## 詳細

- **エントリー条件**:
  - **ロング**: Stochastic RSIが上向きに転じ、直近3値のいずれかが安値ピボット後に`LongEntry`を超える。
  - **ショート**: Stochastic RSIが下向きに転じ、直近3値のいずれかが高値ピボット後に`ShortEntry`を下回る。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 逆のシグナル。
- **ストップ**: なし。
- **デフォルト値**:
  - `RSI Length` = 14
  - `K Length` = 14
  - `D Length` = 3
  - `LongEntry` = 30
  - `ShortEntry` = 60
  - `LongPivot` = 2
  - `ShortPivot` = 98
- **フィルター**:
  - カテゴリ: モメンタム
  - 方向: 両方
  - インジケーター: RSI, Stochastic
  - ストップ: なし
  - 複雑さ: 中程度
  - 時間軸: 短期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
