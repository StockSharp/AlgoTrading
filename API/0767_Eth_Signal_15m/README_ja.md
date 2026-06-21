# ETH Signal 15m戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

ETH Signal 15m戦略は、Supertrendインジケーターを使って方向の変化を検出し、RSIでエントリーをフィルタリングします。SupertrendのDirectionが減少し、RSIが買われすぎレベルを下回るときにロングポジションをオープンします。SupertrendのDirectionが増加し、RSIが売られすぎレベルを上回るときにショートポジションをオープンします。エグジットにはATRベースのストップロスとテイクプロフィットを使用します。

## 詳細

- **エントリー条件**:
  - **ロング**: Supetrendの方向が減少し、RSIが`RsiOverbought`を下回る。
  - **ショート**: Supertrendの方向が増加し、RSIが`RsiOversold`を上回る。
- **ロング/ショート**: 両側。
- **エグジット条件**: ATRベースのストップロスとテイクプロフィット。
- **ストップ**: ストップロス4×ATR、ロングのテイクプロフィット2×ATR、ショートのテイクプロフィット2.237×ATR。
- **デフォルト値**:
  - `AtrPeriod` = 12
  - `Factor` = 2.76
  - `RsiLength` = 12
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: Supertrend, RSI, ATR
  - ストップ: ATRストップロスとテイクプロフィット
  - 複雑さ: 低
  - 時間軸: 15m
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
