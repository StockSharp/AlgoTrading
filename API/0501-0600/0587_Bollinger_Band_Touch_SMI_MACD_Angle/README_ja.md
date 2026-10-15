# Bollinger Bandタッチ と SMI・MACD 角度戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、価格がボリンジャーバンド下限に触れ、SMIとMACDの両方の角度が上向きのときに買います。価格がボリンジャーバンド上限に達するとポジションを決済します。

## 詳細

- **エントリー条件**:
  - **ロング**: 終値がボリンジャーバンド下限に触れるか下回り、SMI/MACDの角度が正でそれぞれの閾値を下回っている。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**:
  - **ロング**: 終値がボリンジャーバンド上限に触れるか超える。
- **ストップ**: なし。
- **デフォルト値**:
  - `BollingerLength` = 20
  - `BollingerMultiplier` = 2.0
  - `SmiLength` = 14
  - `SmiSignalLength` = 3
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `SmiAngleThreshold` = 60
  - `MacdAngleThreshold` = 50
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: ロングのみ
  - インジケーター: Bollinger Bands, Stochastic (SMI), MACD
  - ストップ: なし
  - 複雑さ: 低
  - 時間軸: 1H
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
