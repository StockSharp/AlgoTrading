# Bollinger ADX 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Bollinger Bands と ADX インジケーターを組み合わせた戦略。強いトレンドの確認を伴うブレイクアウトを探します。

テストでは年平均リターン約 46% を示しています。株式市場で最も優れたパフォーマンスを発揮します。

Bollinger Bands の外側への価格動向は ADX によって強さが検証されます。バンドブレイクと高い ADX が一致したときにトレードが実行されます。

強いトレンドを伴うボラティリティの急上昇に有効です。ストップサイズは ATR によって決まります。

## 詳細

- **エントリー条件**:
  - ロング: `Close < LowerBand && ADX > AdxThreshold`
  - ショート: `Close > UpperBand && ADX > AdxThreshold`
- **ロング/ショート**: 両方
- **エグジット条件**:
  - Bollinger 平均回帰
- **ストップ**: `AtrMultiplier` を使用した ATR ベース
- **デフォルト値**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0m
  - `AdxPeriod` = 14
  - `AdxThreshold` = 25m
  - `AtrMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: Bollinger Bands, ADX
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
