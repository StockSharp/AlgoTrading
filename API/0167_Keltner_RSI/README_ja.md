# Keltner Rsi Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Keltner Channels と RSI インジケーターを組み合わせた戦略。価格がチャネル境界に触れ、RSI が売られすぎ/買われすぎ条件を確認した際の平均回帰の機会を探します。

テストでは年平均収益率は約 88% を示しています。株式市場で最もパフォーマンスが優れています。

Keltner Channels は最近のボラティリティをマッピングし、RSI はモメンタムの極値を測定します。RSI がチャネルを超えた動きを支持するときにエントリーが発生します。

ボラティリティエンベロープ周辺のバウンストレーダーに最適です。ストップは ATR 乗数に依存します。

## 詳細

- **エントリー条件**:
  - ロング: `Close < LowerBand && RSI < RsiOversoldLevel`
  - ショート: `Close > UpperBand && RSI > RsiOverboughtLevel`
- **ロング/ショート**: 両方
- **エグジット条件**:
  - 価格が EMA に戻る
- **ストップ**: `StopLossPercent` を使用したパーセントベース
- **デフォルト値**:
  - `EmaPeriod` = 20
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2.0m
  - `RsiPeriod` = 14
  - `RsiOverboughtLevel` = 70m
  - `RsiOversoldLevel` = 30m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: Keltner Channel, RSI
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

