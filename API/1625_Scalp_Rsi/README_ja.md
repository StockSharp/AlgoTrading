# スキャルプ RSI 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

RSIの急速な変化を使用したスキャルピング戦略。MetaTraderスクリプト `scalpen_rsi.mq4` から変換されました。
RSIが急落または急上昇したときに取引を開き、固定のテイクプロフィットとストップロスレベルを適用します。

## 詳細

- **エントリー条件**:
  - **買い**: `buy_period` バー前のRSI値から現在のRSIを引いた値 ≥ `BuyMovement`、
    前のRSIから現在のRSIを引いた値 > `BuyBreakdown`、現在のRSI < `BuyRsiValue`。
  - **売り**: 現在のRSIから `sell_period` バー前のRSIを引いた値 ≥ `SellMovement`、
    現在のRSIから前のRSIを引いた値 > `SellBreakdown`、現在のRSI > `SellRsiValue`。
- **ロング/ショート**: 両方。
- **エグジット条件**: ティック単位の固定テイクプロフィットとストップロス。
- **ストップ**: はい、`BuyStopLoss`、`BuyTakeProfit`、`SellStopLoss`、`SellTakeProfit` を使用。
- **フィルター**:
  - 取引間の最小遅延 (`TradeDelaySeconds`)。
  - 同時オープン取引の最大数 (`MaxOpenTrades`)。
