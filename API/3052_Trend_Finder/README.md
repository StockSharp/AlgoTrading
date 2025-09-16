# Trend Finder Strategy

## Overview
Trend Finder is a multi-timeframe trend-following strategy converted from the original **TREND FINDER.mq4** expert advisor. The logic now uses the high-level StockSharp API and keeps the core idea of combining linear weighted moving averages with confirmations from higher timeframe momentum and MACD filters. The strategy focuses on detecting breakouts that follow sustained highs or lows, aiming to enter in the direction of the breakout once momentum and long-term trend alignment are confirmed.

## Market Data and Indicators
- **Base timeframe (`CandleType`)** – primary candles used for pattern recognition and order execution. The linear weighted moving averages are calculated on the typical price of these candles.
- **Momentum timeframe (`MomentumCandleType`)** – higher timeframe candles used to evaluate momentum deviations from the neutral value of 100. The most recent three momentum readings must exceed configurable thresholds before a trade is allowed.
- **MACD timeframe (`MacdCandleType`)** – long-term candles processed through a MACD with customizable fast, slow, and signal lengths. A bullish (bearish) MACD condition is required for long (short) setups.

## Entry Logic
1. **Trend breakout detection** – the strategy scans up to the last 100 historical candles (excluding the three most recent) to find the highest high or lowest low. A bullish setup requires the current bar to open above a previous cluster of highs while at least one of the previous three highs remains below that historical level. A bearish setup mirrors the logic for lows.
2. **Moving average alignment** – the fast LWMA must be above the slow LWMA for longs and below it for shorts.
3. **Recent candle structure** – for longs the low from two bars ago must be below the high of the previous bar (`Low[2] < High[1]`), while shorts require the latest low to be below the high two bars back (`Low[1] < High[2]`). This preserves the original price-structure check.
4. **Momentum confirmation** – at least one of the last three momentum deviations (computed as |Momentum – 100|) on the higher timeframe must exceed the configured buy/sell thresholds.
5. **MACD confirmation** – the latest MACD value on the long-term timeframe must be above its signal for longs and below it for shorts.
6. **Position filtering** – new long orders are issued only when the current position is non-positive, and new short orders only when it is non-negative. Order volume equals `Volume + |Position|` to support fast position reversals.

## Exit and Risk Management
- **Stop-loss (`StopLoss`)** – fixed distance below (above) the entry price for long (short) positions.
- **Take-profit (`TakeProfit`)** – fixed profit target; when reached the position is closed immediately.
- **Trailing stop (`TrailingStop`)** – trails the highest price reached after entering a long or the lowest price for shorts. The stop is adjusted on every finished candle.
- **Break-even (`BreakEvenTrigger`, `BreakEvenOffset`)** – once price moves in favor of the trade by the trigger distance, the protective stop is moved to the entry price plus (minus) the offset for longs (shorts), ensuring profits are locked in if price retraces.
- **Automatic flattening** – helper methods close the entire position size, then reset all tracking variables. There are no partial exits in this implementation.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `CandleType` | Base timeframe for pattern recognition and order execution. | 15-minute candles |
| `MomentumCandleType` | Higher timeframe used to compute momentum confirmation. | 1-hour candles |
| `MacdCandleType` | Timeframe for MACD confirmation (defaults to ~30-day candles). | 30-day candles |
| `FastMaLength` | Length of the fast linear weighted moving average. | 6 |
| `SlowMaLength` | Length of the slow linear weighted moving average. | 85 |
| `MomentumPeriod` | Number of higher-timeframe bars used for the momentum ratio. | 14 |
| `MomentumThresholdBuy` | Minimum |Momentum − 100| required to allow long entries. | 0.3 |
| `MomentumThresholdSell` | Minimum |Momentum − 100| required to allow short entries. | 0.3 |
| `MacdShortLength` | Fast EMA length used inside the MACD calculation. | 12 |
| `MacdLongLength` | Slow EMA length used inside the MACD calculation. | 26 |
| `MacdSignalLength` | Signal EMA length for MACD. | 9 |
| `StopLoss` | Absolute stop-loss distance in instrument price units. | 0.0020 |
| `TakeProfit` | Absolute take-profit distance in instrument price units. | 0.0050 |
| `TrailingStop` | Trailing stop distance that follows favorable moves. | 0.0040 |
| `BreakEvenTrigger` | Profit distance that activates the break-even stop. | 0.0030 |
| `BreakEvenOffset` | Additional offset applied once break-even is active. | 0.0010 |

> **Note:** Set the `Strategy.Volume` property to the desired order size before starting the strategy. The parameters above are expressed in absolute price units; adjust them according to the traded instrument's tick size.

## Usage Guidelines
1. Assign the strategy to the desired `Security` and configure the `Portfolio` and `Volume` properties.
2. Ensure that the selected data source can deliver all three requested candle timeframes; otherwise, the confirmation filters will never become ready.
3. Adjust risk parameters to match the instrument's volatility. Because the defaults are expressed as absolute price distances, they may require rescaling for equities, futures, or crypto.
4. Optionally attach the generated chart area to visualize price, trades, and both moving averages.
5. Monitor logs for order confirmations. The strategy uses market orders (`BuyMarket`, `SellMarket`) for entries and exits.

## Differences from the Original Expert Advisor
- Equity-based stops, balance-based take-profit logic, and push/email notifications present in the MQL script were intentionally omitted to keep the strategy focused on core trading rules and to align with the StockSharp high-level API.
- Volume management is simplified: the StockSharp version opens at most one net position at a time and uses the configured `Volume` to size trades.
- Money-management parameters expressed in account currency are not replicated; instead, price-based risk controls (`StopLoss`, `TakeProfit`, `TrailingStop`, break-even) are provided.

## Recommended Enhancements
- Add portfolio-level risk controls if trading multiple symbols simultaneously.
- Combine with session filters or volatility filters to disable trading during illiquid periods.
- Consider piping fills to external analytics (e.g., for equity tracking) if such functionality is required.

