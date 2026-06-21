# MACD と 1D Stochastic 確認によるリバーサル戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

MACD ラインがシグナルラインを上抜け、日足 Stochastic オシレーターで確認が取れたときに買いエントリーする戦略。ATR ベースのストップロスを価格が下抜けるか、トレーリング EMA テイクプロフィットを下回ったときに決済します。

## 詳細

- **エントリー条件**:
  - ロング: `MACD crosses above Signal && DailyK > DailyD && DailyK < 80`
- **ロング/ショート**: ロングのみ
- **ストップ**: ATR ストップロスとトレーリング EMA テイクプロフィット
- **デフォルト値**:
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
  - `TrailingEmaLength` = 20
  - `StopLossAtrMultiplier` = 3.25m
  - `TrailingActivationAtrMultiplier` = 4.25m
  - `CandleType` = TimeSpan.FromHours(1).TimeFrame()
- **フィルター**:
  - カテゴリ: リバーサル
  - 方向: ロング
  - インジケーター: MACD、Stochastic、ATR、EMA
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
