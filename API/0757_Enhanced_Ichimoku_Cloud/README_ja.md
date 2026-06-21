# 強化版一目均衡表クラウド戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

171日EMAフィルターを備えたロングオンリーのIchimoku戦略。スパンAがスパンBを上回り、価格が25バー前の高値を突破し、転換線が基準線を上回り、終値がEMAを上回ったときに買います。転換線が基準線を下回ったときにポジションをクローズします。

## 詳細

- **エントリー条件**: spanA > spanB, close > high[25], Tenkan > Kijun, close > EMA。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**: Tenkan < Kijun。
- **ストップ**: なし。
- **デフォルト値**:
  - `ConversionPeriods` = 7
  - `BasePeriods` = 211
  - `LaggingSpan2Periods` = 120
  - `Displacement` = 41
  - `EmaPeriod` = 171
  - `StartDate` = 2018-01-01
  - `EndDate` = 2069-12-31
  - `CandleType` = TimeSpan.FromDays(1)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: ロングのみ
  - インジケーター: Ichimoku, EMA
  - ストップ: なし
  - 複雑さ: 基本
  - 時間軸: 日足
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
