# FIBO1 Strategy (MQL 24845 Conversion)

## Overview

The **FIBO1 Strategy** reproduces the trading rules of the original `FIBO1.mq4` expert
advisor by Aharon Tzadik (MQL script 24845) using the StockSharp high-level API. The
strategy trades a single symbol on a selected timeframe and combines three groups of
filters:

1. **Trend filter** – a fast and a slow Linear Weighted Moving Average (LWMA) of the
   typical price. Long signals require the fast LWMA to stay above the slow LWMA,
   while shorts require the inverse relationship.
2. **Momentum confirmation** – three consecutive momentum readings are compared
   against user-defined buy/sell thresholds. The algorithm mimics the original
   absolute deviation from 100 that the MQL code used on higher timeframes.
3. **MACD filter** – a higher timeframe MACD must confirm the trade direction. The
   StockSharp port keeps the 12/26/9 defaults and checks the relationship between the
   MACD main and signal lines exactly as in the expert advisor.

Once a position is active, the strategy recreates the sophisticated exit logic from
`FIBO1.mq4`:

- Traditional pip-based stop loss and take profit distances.
- Optional money/percentage based take profit and trailing targets.
- Candle-based trailing stops that follow recent highs/lows, including an additional
  price buffer identical to the "PAD AMOUNT" setting.
- Classic trailing distances that activate after a minimum profit threshold.
- Automatic break-even protection with an offset expressed in pips.
- An equity stop that monitors floating drawdown against the historical equity peak.

> **Note:** The original MQL expert relied on a manually drawn "FIBO" line on the
> chart for live trading. StockSharp strategies cannot access terminal drawing
> objects, therefore the port always behaves like the testing branch of the MQL
> code (the part that ignores the Fib retracement filter). All other features are
> preserved and the documentation below explains every available parameter.

## Trading Logic

1. **Signal detection**
   - Wait for a finished candle on the primary timeframe.
   - Ensure the fast LWMA is above (long) or below (short) the slow LWMA.
   - Check the price pattern that compares the previous candle's high/low pair,
     mirroring `Low[2] < High[1]` for longs and `Low[1] < High[2]` for shorts.
   - Evaluate the maximum absolute deviation of the last three momentum values from
     the neutral 100 level. If it exceeds the configured threshold, the momentum
     filter passes.
   - Confirm that the higher timeframe MACD main line stays above (long) or below
     (short) its signal line.
   - When every filter aligns, reverse any opposite exposure and open a market order
     using the configured trade volume.

2. **Risk management**
   - Each new position immediately receives pip-based stop loss and take profit
     orders through the StockSharp protective API.
   - Break-even logic tightens the stop once the floating profit equals the activation
     threshold.
   - Price-based trailing can operate in two modes: (a) follow candle extremes with a
     pad offset, or (b) maintain a fixed pip distance after the trade moves into
     profit.
   - A money-management module handles cash-based targets, percent-of-equity targets,
     and a floating-profit trailing stop identical to the original EA.
   - The global equity stop continuously tracks the highest equity level observed
     since the start and closes all positions when the maximum allowed drawdown is
     breached.

## Parameters

| Name | Default | Description |
|------|---------|-------------|
| `UseMoneyTakeProfit` | `false` | Close all positions when unrealized profit reaches `MoneyTakeProfit` (account currency). |
| `MoneyTakeProfit` | `10` | Profit target in account currency. Effective only if `UseMoneyTakeProfit = true`. |
| `UsePercentTakeProfit` | `false` | Enable a profit target expressed as a percentage of the initial equity snapshot. |
| `PercentTakeProfit` | `10` | Percentage used by the equity-based profit target. |
| `EnableMoneyTrailing` | `true` | Activates money-based trailing once unrealized profit reaches `MoneyTrailTarget`. |
| `MoneyTrailTarget` | `40` | Minimum floating profit that enables the money trailing logic. |
| `MoneyTrailStop` | `10` | Maximum permissible drawdown (in currency units) after money trailing activates. |
| `UseEquityStop` | `true` | Enable the global equity drawdown protection. |
| `EquityRiskPercent` | `1` | Maximum drawdown (percent of peak equity) before all positions are closed. |
| `TradeVolume` | `1` | Base volume (lots/contracts) for market entries. |
| `FastMaPeriod` | `20` | Period of the fast LWMA calculated on the typical price. |
| `SlowMaPeriod` | `100` | Period of the slow LWMA calculated on the typical price. |
| `MomentumPeriod` | `14` | Length of the momentum indicator used by the confirmation filter. |
| `MomentumBuyThreshold` | `0.3` | Minimum absolute deviation from 100 required for long trades. |
| `MomentumSellThreshold` | `0.3` | Minimum absolute deviation from 100 required for short trades. |
| `MacdFastPeriod` | `12` | Fast EMA length inside the higher timeframe MACD. |
| `MacdSlowPeriod` | `26` | Slow EMA length inside the higher timeframe MACD. |
| `MacdSignalPeriod` | `9` | Signal EMA length inside the higher timeframe MACD. |
| `TakeProfitPips` | `50` | Protective take profit distance in pips. |
| `StopLossPips` | `20` | Protective stop loss distance in pips. |
| `TrailingActivationPips` | `40` | Minimum profit (pips) required before pip-based trailing activates. |
| `TrailingDistancePips` | `40` | Distance maintained by the price-based trailing stop. |
| `UseCandleTrailing` | `true` | When enabled, the trailing stop follows recent candle extremes instead of using a fixed distance. |
| `CandleTrailingLength` | `3` | Number of finished candles used to compute the trailing extreme. |
| `CandleTrailingOffsetPips` | `3` | Additional pip buffer added to the candle trailing price. |
| `MoveToBreakEven` | `true` | Enable the break-even protection. |
| `BreakEvenActivationPips` | `30` | Profit (pips) required before the stop moves to break-even. |
| `BreakEvenOffsetPips` | `30` | Offset (pips) added beyond the entry price when the stop moves to break-even. |
| `CandleType` | `15m` | Primary candle series used for the trading signals. |
| `MomentumCandleType` | `15m` | Candle series feeding the momentum indicator. |
| `MacdCandleType` | `1d` | Higher timeframe series used by the MACD filter. |

## Usage Notes

- The default candle types mirror the multi-timeframe logic from the expert advisor:
  the main and momentum series use the chart timeframe, while the MACD works on a
  higher timeframe (daily by default). All three series can be reconfigured.
- The pip conversion routine automatically accounts for 3/5 decimal forex symbols by
  multiplying the price step by 10. Instruments with other tick sizes keep the raw
  `PriceStep` multiplier.
- The strategy relies exclusively on finished candles. Ensure that the connected data
  provider publishes candle states, otherwise the entry conditions will never trigger.
- When the symbol trades in a netting environment, position reversals are executed by
  closing the opposite exposure before opening a new trade, exactly as the original
  EA did with market orders.

## Differences from the Original EA

- Fib retracement object checks are not present because StockSharp cannot access MT4
  chart drawings. The strategy always behaves like the tester branch of the MQL code.
- Money management parameters (`Lots`, `LotExponent`, and `Max_Trades`) were replaced
  with a single `TradeVolume` property because StockSharp strategies operate on net
  positions. Volume scaling can be scripted externally via optimizers if required.
- All logging and alerting routines (`Alert`, `SendMail`, `SendNotification`) were
  intentionally removed to keep the StockSharp version self-contained.

With these adjustments the StockSharp port remains faithful to the trading logic of
`FIBO1.mq4` while providing a clean, parameterized implementation that integrates with
other AlgoTrading samples.
