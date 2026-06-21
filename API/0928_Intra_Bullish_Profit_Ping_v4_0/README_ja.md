# イントラブリッシュ Profit Ping v4.0 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

MACDヒストグラムとRSI強度で確認されたEMAクロスオーバーを使用するロングのみのシステム。

## 詳細

- **エントリー条件**:
  - 短期EMAが長期EMAを上抜け
  - MACDヒストグラム > 0
  - RSI > 50
  - 終値 > 始値
- **エグジット条件**:
  - 短期EMAが長期EMAを下抜け
  - MACDヒストグラム < 0
  - RSI < 50
  - 終値 < 始値
- **インジケーター**:
  - 指数移動平均
  - MACD
  - RSI
- **ストップ**: なし。
- **デフォルト値**:
  - `ShortEmaLength` = 7
  - `LongEmaLength` = 14
  - `RsiLength` = 14
  - `MacdFastPeriod` = 12
  - `MacdSlowPeriod` = 26
  - `MacdSignalPeriod` = 9
- **フィルター**:
  - トレンドフォロー
  - 単一時間軸
  - インジケーター: EMA, MACD, RSI
  - ストップ: なし
  - 複雑さ: 低
