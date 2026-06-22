# Exp QqeCloud戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

平滑化されたRSIにQQE（Quantitative Qualitative Estimation）インジケーターを適用するトレンドフォローアプローチ。
戦略は事前定義されたセッション開始時刻にのみポジションを開き、反対のシグナルが発生するか
取引セッションが終了するときにポジションを閉じます。

## 詳細

- **エントリー条件**:
  - **ロング**: `StartHour`:`StartMinute`において、QQEトレンドが上向きに転換。
  - **ショート**: `StartHour`:`StartMinute`において、QQEトレンドが下向きに転換。
- **エグジット条件**:
  - 反対のQQEトレンドシグナル。
  - 時刻が`StopHour`:`StopMinute`を超える。
- **インジケーター**:
  - RSI（期間`RsiPeriod`、`RsiSmoothing`で平滑化）。
  - 乗数`QqeFactor`を使用したQQEバンド。
- **ストップ**: デフォルトではなし。
- **デフォルト値**:
  - `CandleType` = 1分足
  - `RsiPeriod` = 14
  - `RsiSmoothing` = 5
  - `QqeFactor` = 4.236
  - `StartHour` = 0, `StartMinute` = 0
  - `StopHour` = 23, `StopMinute` = 59
- **フィルター**:
  - エントリーとエグジットの時間ウィンドウ
  - トレンドフォロー、単一時間軸
