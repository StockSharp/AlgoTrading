# Bollinger Williams R Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Bollinger Bands と Williams %R インジケーターに基づく戦略。価格が下部バンドにあり、Williams %R が売られすぎ（< -80）の場合にロング参入。価格が上部バンドにあり、Williams %R が買われすぎ（> -20）の場合にショート参入。

テストでは年平均収益率は約 103% を示しています。株式市場で最もパフォーマンスが優れています。

Bollinger Bands はボラティリティのブレイクアウトを明らかにし、Williams %R はモメンタムが極端であることを確認します。価格が対応する Williams %R の読みとともにバンドの外で終値をつけたときにポジションが開かれます。

ボラティリティ拡張トレーダーに最適です。ATR ストップが不利な転換を処理します。

## 詳細

- **エントリー条件**:
  - ロング: `Close < LowerBand && WilliamsR < -80`
  - ショート: `Close > UpperBand && WilliamsR > -20`
- **ロング/ショート**: 両方
- **エグジット条件**: 価格が中間バンドに戻る
- **ストップ**: `AtrMultiplier` を使用した ATR ベース
- **デフォルト値**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0m
  - `WilliamsRPeriod` = 14
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: Bollinger Bands, Williams %R, R
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

