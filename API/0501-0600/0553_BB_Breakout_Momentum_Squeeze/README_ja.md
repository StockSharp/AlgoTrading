# BB ブレイクアウト・モメンタム・スクイーズ戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

BB Breakout Momentum Squeeze戦略は、Bollinger Bandsのブレイクアウトオシレーターとボラティリティスクイーズフィルターを組み合わせます。Bollinger BandsがKeltner Channelsの外に出たときにスクイーズが検出され、潜在的な拡大を示します。この拡大中に強気ブレイクアウトオシレーターが閾値を上回るとロングトレードが発生し、弱気クロスはショートトレードを使用します。ストップはATRバンドに基づき、リスク-リワード目標がエグジットロジックを完成させます。

## 詳細

- **エントリー条件**:
  - スクイーズオフ（Bollinger BandsがKeltner Channelsの外側）。
  - **ロング**: 強気オシレーターが閾値を上回る。
  - **ショート**: 弱気オシレーターが閾値を上回る。
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - 価格がATRストップまたはリスク-リワード目標に達する。
- **ストップ**: ATRバンドとリスク-リワード目標。
- **デフォルト値**:
  - `BbLength` = 14
  - `BbMultiplier` = 1.0
  - `Threshold` = 50
  - `SqueezeLength` = 20
  - `SqueezeBbMultiplier` = 2.0
  - `KcMultiplier` = 2.0
  - `AtrLength` = 30
  - `AtrMultiplier` = 1.4
  - `RrRatio` = 1.5
- **フィルター**:
  - カテゴリ: ボラティリティブレイクアウト
  - 方向: 両方
  - インジケーター: Bollinger Bands, Keltner Channels, ATR
  - ストップ: あり
  - 複雑さ: 中程度
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
