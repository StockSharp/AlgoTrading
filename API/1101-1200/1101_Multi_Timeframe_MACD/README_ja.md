# マルチ時間軸 MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

マルチ時間軸 MACD は、作業時間軸と上位時間軸の MACD シグナルを組み合わせます。ラインクロスオーバーまたはゼロラインクロスで両時間軸が一致したときにエントリーします。

## 詳細
- **データ**: 2 つの時間軸の価格ローソク足。
- **エントリー条件**:
  - **ロング**: `Entry` パラメーターによります。デフォルトでは両時間軸での強気クロスオーバー。
  - **ショート**: ロングの反対。
- **エグジット条件**: 逆シグナルまたはトレーリングストップ。
- **ストップ**: オプションのトレーリングストップ。
- **デフォルト値**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `CandleType` = tf(5)
  - `HigherCandleType` = tf(1d)
  - `ShowCurrentTimeframe` = true
  - `ShowHigherTimeframe` = true
  - `Entry` = Crossover
  - `UseTrailingStop` = false
  - `TrailingStopPercent` = 2
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: ロング/ショート
  - インジケーター: MACD
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: マルチ時間軸 (5m/1d)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
