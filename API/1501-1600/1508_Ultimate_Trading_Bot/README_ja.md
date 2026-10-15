# Ultimate Trading Bot 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

RSI、移動平均、MACD、ストキャスティクスのクロスオーバーを組み合わせてエントリーとエグジットのタイミングを計るロングのみの戦略。

## 詳細

- **エントリー条件**: 価格がMAを上回っている状態でRSIが売られすぎゾーンを上抜け、MACDとストキャスティクスが上方クロス。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**: 逆のクロス条件。
- **ストップ**: 明示的なストップなし。
- **デフォルト値**:
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `MaLength` = 50
  - `StochLength` = 14
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
- **フィルター**:
  - カテゴリ: モメンタム
  - 方向: ロング
  - インジケーター: RSI, MA, MACD, Stochastic
  - ストップ: いいえ
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
