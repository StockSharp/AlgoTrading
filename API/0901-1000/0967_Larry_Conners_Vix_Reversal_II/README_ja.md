# Larry Conners VIXリバーサルII戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

VIX指数のRSIに基づいて取引する戦略です。VIXのRSIが買われすぎレベルを上抜けるとロングポジションを開きます。RSIが売られすぎレベルを下抜けるとショートポジションを開きます。ポジションは最小保有日数が経過した後に決済されます。

## 詳細

- **エントリー条件**:
  - **ロング**: RSI(VIX)が`Overbought level`を上抜ける。
  - **ショート**: RSI(VIX)が`Oversold level`を下抜ける。
- **ロング/ショート**: 両方向。
- **エグジット条件**: `Min holding days`から`Max holding days`の経過後にポジション決済。
- **ストップ**: なし。
- **デフォルト値**:
  - `RSI period` = 25
  - `Overbought level` = 61
  - `Oversold level` = 42
  - `Min holding days` = 7
  - `Max holding days` = 12
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: RSI
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: 日足
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
