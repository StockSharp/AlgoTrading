# Macd Williams R Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

MACD と Williams %R インジケーターに基づく戦略。MACD > Signal かつ Williams %R が売られすぎ（< -80）の場合にロング参入。MACD < Signal かつ Williams %R が買われすぎ（> -20）の場合にショート参入。

テストでは年平均収益率は約 100% を示しています。外国為替市場で最もパフォーマンスが優れています。

MACD はより大きなモメンタムの転換を示し、Williams %R は短期的な反転を正確に特定します。両方のシグナルが一致する必要があります。

トレンドとカウンタートレンドの両方のシグナルを組み合わせたい人に適しています。ストップは ATR 係数に依存します。

## 詳細

- **エントリー条件**:
  - ロング: `MACD > Signal && WilliamsR < -80`
  - ショート: `MACD < Signal && WilliamsR > -20`
- **ロング/ショート**: 両方
- **エグジット条件**: 反対方向への MACD クロス
- **ストップ**: `StopLossPercent` を使用したパーセントベース
- **デフォルト値**:
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `WilliamsRPeriod` = 14
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: MACD, Williams %R, R
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

