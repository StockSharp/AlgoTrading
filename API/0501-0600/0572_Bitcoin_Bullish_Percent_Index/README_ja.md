# Bitcoin ブリッシュ・パーセント・インデックス戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はRSI（相対力指数）を使ってBitcoin Bullish Percent Indexを近似します。RSIが売られすぎレベルを上抜けるとロングに入り、買われすぎレベルを下抜けるとショートに入ります。

## 詳細

- **エントリー条件**:
  - **ロング**: RSIが売られすぎレベルを上抜け。
  - **ショート**: RSIが買われすぎレベルを下抜け。
- **ロング/ショート**: 両方。
- **エグジット条件**: 反対のシグナル。
- **ストップ**: いいえ。
- **デフォルト値**:
  - `RSI Period` = 14
  - `Overbought` = 70
  - `Oversold` = 30
- **フィルター**:
  - カテゴリ: オシレーター
  - 方向: 両方
  - インジケーター: RSI
  - ストップ: いいえ
  - 複雑さ: 低
  - 時間軸: 中期
