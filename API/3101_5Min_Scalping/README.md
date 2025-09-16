# 5MIN Scalping Strategy

Conversion of the MT4 expert advisor **"5MIN SCALPING" (MQL ID 22828)** to the StockSharp high level API. The strategy searches for fast breakout setups on the primary timeframe and confirms them with higher timeframe momentum and monthly MACD direction before entering the market.

- **Category:** Breakout / Momentum scalping
- **Original platform:** MetaTrader 4
- **Data requirements:** Tick or candle feed for the configured timeframes (default 5 minutes, 30 minutes, 1 month)

## Trading Logic

1. **Trend filter.** Two linear weighted moving averages (LWMA) with configurable lengths (default 6 and 85) define the prevailing trend. Longs require the fast LWMA to stay above the slow LWMA, shorts require the opposite relation.
2. **Multi-bar structure filter.** Internal LWMA triplet (lengths 8, 13, 21) is evaluated on the last 20 completed candles. The algorithm mimics the `scalper()` function from the MQL version:
   - Bullish setup: every bar inside the loop must satisfy `LWMA8 > LWMA13 > LWMA21`, the candle low pulls back into the moving average stack, and the current close breaks above the highest high of the previous 5 candles.
   - Bearish setup: mirror logic using highs penetrating the LWMA stack and the current close breaking below the lowest low of the previous 5 candles.
3. **Candlestick overlap guard.** A minor overlap condition (`Low[2] < High[1]` for longs, `Low[1] < High[2]` for shorts) prevents entries in isolated spikes.
4. **Momentum confirmation.** A higher timeframe `Momentum` indicator (default 30-minute candles, length 14) must show that at least one of the last three values deviates from the 100 baseline by more than the configured thresholds (0.3 by default).
5. **Macro MACD alignment.** A monthly `MACD(12, 26, 9)` histogram is calculated via `MovingAverageConvergenceDivergenceSignal`. Long trades require the MACD line to be above the signal line, short trades require the opposite.
6. **Position aggregation.** Entering in the opposite direction closes the existing exposure first and immediately opens the new trade with the configured volume.

## Risk Management

- **Static targets.** Optional take-profit and stop-loss levels in pips (converted internally using the instrument `PriceStep`).
- **Break-even module.** When enabled, the stop is moved to entry ± offset once price travels a configurable number of pips.
- **Trailing stop.** Optional trailing stop that follows the position by a fixed pip distance once the market advances.
- **Manual exits.** All exits are handled inside the strategy without placing protective orders, which mirrors the original EA behaviour.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `CandleType` | 5-minute timeframe | Primary timeframe where the breakout is detected. |
| `MomentumCandleType` | 30-minute timeframe | Candle type used for the higher timeframe momentum filter. |
| `MacroMacdCandleType` | 1-month timeframe | Candle type used for the long-term MACD confirmation. |
| `FastMaLength` | 6 | Length of the fast LWMA trend filter. |
| `SlowMaLength` | 85 | Length of the slow LWMA trend filter. |
| `MomentumLength` | 14 | Lookback for the momentum indicator. |
| `MomentumBuyThreshold` | 0.3 | Minimum deviation |Momentum-100| needed to confirm long trades. |
| `MomentumSellThreshold` | 0.3 | Minimum deviation |Momentum-100| needed to confirm short trades. |
| `TakeProfitPips` | 50 | Take-profit distance expressed in pips. Set to 0 to disable. |
| `StopLossPips` | 20 | Stop-loss distance expressed in pips. Set to 0 to disable. |
| `TrailingStopPips` | 40 | Trailing stop distance in pips. Effective only when `EnableTrailing` is true. |
| `EnableTrailing` | true | Turns the trailing stop logic on or off. |
| `EnableBreakEven` | true | Enables automatic break-even management. |
| `BreakEvenTriggerPips` | 30 | Profit in pips needed before the stop is moved to break-even. |
| `BreakEvenOffsetPips` | 30 | Extra buffer (in pips) added when the stop is shifted to break-even. |
| `TradeVolume` | 1 | Order volume used for entries. |

## Usage

1. Add the strategy to your StockSharp project and link it to the desired instrument.
2. Ensure that historical data for all configured candle types is available before starting the strategy.
3. Configure volume, timeframes and thresholds according to the traded instrument volatility.
4. Start the strategy. It will subscribe to all required candle series, draw indicators on the chart (when charting is available), and manage entries/exits automatically.

## Notes and Differences vs. the Original EA

- The money-based trailing modules (`Take_Profit_In_Money`, `TRAIL_PROFIT_IN_MONEY2`) and equity stop from the MQL version are not ported. Risk is handled through pip-based distances instead.
- Martingale-style lot scaling (`Lots * MathPow(LotExponent, CountTrades())`) is not implemented. Adjust `TradeVolume` manually if you need position sizing.
- Email/notification alerts present in the original code are omitted. Use StockSharp notification infrastructure if required.
- The strategy relies on the instrument `PriceStep` to convert pip distances. Validate that the instrument metadata is populated correctly in your environment.
