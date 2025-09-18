# Cryptos Strategy

## Overview

The **Cryptos Strategy** is a high-level StockSharp port of the original MetaTrader4 expert advisor `cryptos.mq4`. It focuses on the ETH/USD pair, combining Bollinger Bands with a linear weighted moving average (LWMA) to capture breakouts from volatility compression. The strategy tracks swing highs and lows across a configurable number of candles and dynamically calculates take-profit targets as a multiple of the detected range.

## Trading Logic

1. **Trend detection** – when the close price touches the upper Bollinger band the strategy switches into a short bias, and when the lower band is touched it switches into a long bias. The band touch also freezes the current swing values by disabling automatic high/low updates.
2. **Entry conditions** –
   - Open a short position when the close price falls below the LWMA, the bias is short, and there is no active short position.
   - Open a long position when the close price rises above the LWMA, the bias is long, and there is no active long position.
3. **Range projection** – swing highs and lows (either automatic or manually frozen) define the distance from the LWMA. This distance, expressed in ticks, is multiplied by the take-profit ratio to compute profit targets and the risk-based position size.
4. **Risk control** – the strategy sets per-trade take-profit and stop-loss levels. For longs, the stop is placed below the swing low; for shorts, above the swing high. Stops and targets are recalculated for each entry and enforced inside the strategy loop.
5. **Trailing exits** – if a long position closes below the lower Bollinger band (or a short above the upper band), the open position is flattened immediately, mimicking the trailing behaviour of the original EA.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `CandleType` | Data type of the candle series used for all indicator calculations. |
| `BollingerPeriod`, `BollingerWidth` | Length and standard deviation multiplier of the Bollinger Bands. |
| `MaPeriod` | Period of the linear weighted moving average based on median prices. |
| `LookbackCandles` | Number of candles examined to determine the automatic swing high/low. |
| `TakeProfitRatio` | Range multiplier used for profit targets when trading ETH/USD. |
| `AlternativeTakeProfitRatio` | Range multiplier applied to all other symbols. |
| `RiskPerTrade` | Amount of capital (in quote currency) that the volume calculator tries to risk on each trade. |
| `ValueIndex`, `CryptoValueIndex` | Multipliers converting risk into volume for non-crypto and crypto symbols respectively. |
| `MinVolume`, `MaxVolume` | Hard bounds for position size after alignment to exchange volume steps. |
| `MinRangeTicks` | Minimum allowed projected range in ticks to avoid zero-distance stops. |
| `SpreadPoints` | Manual override of the spread in ticks (auto-detected from best bid/ask if available). |
| `GlobalTrend` | Manual bias override: `1` forces a short setup, `2` forces a long setup, `0` lets the strategy decide. |
| `AutoHighLow` | When enabled, swing points are recalculated on every candle; when disabled they are frozen until the next band touch. |
| `ManualBuyTrigger`, `ManualSellTrigger` | Set to `true` to request an immediate long or short entry (reset after execution). |
| `SkipBuys`, `SkipSells` | Disable opening new long or short positions. |

## Position Sizing

The strategy replicates the MT4 logic: `volume = RiskPerTrade / rangeTicks * valueIndex`. The result is aligned to `VolumeStep`, then clipped between `MinVolume`/`MaxVolume` and the instrument’s exchange-imposed limits.

## Usage Notes

- The strategy checks portfolio value on start. If the balance is lower than `RiskPerTrade * 3`, trading is disabled and a warning is logged, matching the EA’s safety check.
- Manual triggers and bias controls make it possible to synchronise with discretionary decisions during live trading.
- ETH/USD automatically uses `CryptoValueIndex` and `TakeProfitRatio`; other instruments fall back to the alternative parameters.
- Stops and targets are enforced within the strategy loop, so no additional protection module is required.

