# SJ NIFTY戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

SuperTrend、VWAP、RSI、EMA200を使用したトレンドフォロー戦略。Keltnerチャネルのベースはオプションのトレンドフィルターとして機能します。ポジションサイズは資本のリスク割合からストップロスとリスクリワードベースのテイクプロフィットで算出されます。

## 詳細

- **エントリー条件**:
  - **ロング**: 終値 > SuperTrend && 終値 > VWAP && RSI > 買われすぎ && 終値 > EMA200 && Keltnerベースフィルター && 終値 > 前の高値。
  - **ショート**: 終値 < SuperTrend && 終値 < VWAP && RSI < 売られすぎ && 終値 < EMA200 && Keltnerベースフィルター && 終値 < 前の安値。
- **エグジット条件**: ストップロスまたはリスク比率に基づくテイクプロフィット。
- **ポジションサイジング**: ポートフォリオのリスク割合をストップ距離で割り、ロットサイズに丸めた値。
- **インジケーター**: SuperTrend, VWAP, RSI, EMA, Keltner Channels.
