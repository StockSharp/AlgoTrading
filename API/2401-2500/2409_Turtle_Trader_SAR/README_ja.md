# Turtle Trader SAR 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Turtle Trader SAR は、オプションの Parabolic SAR トレーリングを備えた元の MQL5 Turtle システムを StockSharp C# に変換したものです。
この戦略は Donchian チャネルのブレイクアウトを取引し、ATR ベースのリスクでポジションサイズを決定し、勝ちトレードをピラミッドできます。

## 仕組み

1. **インジケーター計算**
   - ボラティリティのための 20 期間 ATR。
   - `ShortPeriod` と `ExitPeriod` の Donchian チャネル。
   - トレーリングストップのためのオプション Parabolic SAR。
2. **ポジションサイジング**
   - 各エントリーは現在の純資産の `RiskFraction` をリスクにさらします。
   - ユニットサイズは `MaxUnits` で制限されます。
3. **エントリー条件**
   - `ShortPeriod` の高値を上回る終値 -> 買い。
   - `ShortPeriod` の安値を下回る終値 -> 売り。
4. **ピラミッディング**
   - `MaxUnits` に達するまで、有利な方向への `AddInterval` ATR の動きごとに新しいユニットを追加。
5. **エグジット条件**
   - 逆方向の `ExitPeriod` ブレイクアウト。
   - `StopAtr` を使った ATR ストップとオプションのテイクプロフィット `TakeAtr`。
   - `UseSar` が true の場合、Parabolic SAR ストップも適用されます。

## パラメーター

- `ExitPeriod` = 10
- `ShortPeriod` = 20
- `LongPeriod` = 55
- `RiskFraction` = 0.01
- `MaxUnits` = 4
- `AddInterval` = 1
- `StopAtr` = 1
- `TakeAtr` = 1
- `UseSar` = false
- `SarStep` = 0.02
- `SarMax` = 0.2
- `CandleType` = 1 day

## タグ

- **カテゴリ**: トレンドフォロー
- **方向**: 両方
- **インジケーター**: ATR, Highest, Lowest, Parabolic SAR
- **ストップ**: ATR / SAR
- **複雑さ**: 中級
- **時間軸**: 日足
- **季節性**: いいえ
- **ニューラルネットワーク**: いいえ
- **ダイバージェンス**: いいえ
- **リスクレベル**: 中
