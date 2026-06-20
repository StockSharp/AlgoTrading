# Omar MMR戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

RSI、3本の指数移動平均、MACDクロスオーバーを組み合わせたモメンタムベースの手法です。価格が遅いEMAを上回り、速いEMAが中間EMAを超え、MACDが強気にクロスし、RSIが29から70の中立ゾーンにある時にロング取引が発生します。

テイクプロフィットとストップロスのパーセンテージはエンジンの保護モジュールを通じて適用されます。この設定はモメンタムとトレンドを整合させながら、RSIの過度な延伸を避けることに焦点を当てています。

## 詳細

- **エントリー条件**:
  - **ロング**: EMA Cの上で終値が付き、EMA A > EMA B、MACDラインがシグナルを上抜け、RSIが29から70の間。
- **エグジット条件**:
  - テイクプロフィットまたはストップロスで管理；明示的なインジケーターエグジットなし。
- **インジケーター**:
  - RSI (長さ14)
  - EMA A/B/C (期間20/50/200)
  - MACD (12,26,9)
- **ストップ**: パーセントベースのテイクプロフィット1.5%とストップロス2%がデフォルト。
- **デフォルト値**:
  - `RsiLength` = 14
  - `EmaALength` = 20
  - `EmaBLength` = 50
  - `EmaCLength` = 200
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
  - `TakeProfitPercent` = 1.5
  - `StopLossPercent` = 2.0
- **フィルター**:
  - トレンド継続
  - 単一時間軸
  - インジケーター: RSI、EMA、MACD
  - ストップ: はい
  - 複雑さ: 中程度
