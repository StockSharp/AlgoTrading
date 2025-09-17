# BreakRevert Pro Strategy

BreakRevert Pro is the StockSharp conversion of the MetaTrader 5 expert advisor *BreakRevertPro.mq5*. The strategy combines breakout confirmation on the one-minute timeframe with broader trend and volatility context from the 15-minute and one-hour charts. Probability-style scores are reproduced through indicator-driven approximations so that the behaviour remains close to the original EA while following StockSharp high-level API patterns.

## Core Logic

1. **Primary timeframe (1 minute)**
   - Average True Range (ATR) estimates intraday volatility.
   - A moving average of close prices measures the short-term directional bias.
   - A second moving average tracks the frequency of large candle-to-candle moves, representing the Poisson breakout probability from the MQL code.
   - An exponential moving average of absolute price moves produces the exponential-style probability used by the original safety filter.
2. **Confirmation timeframe (15 minutes)**
   - A simple moving average measures the medium-term trend direction and blocks trades against the dominant flow.
3. **Context timeframe (1 hour)**
   - Hourly candles provide the higher timeframe trend and the volatility range required for breakout validation and mean-reversion flattening checks.

When the Poisson and Weibull proxy probabilities exceed the breakout threshold, the 1-minute and 15-minute trends are aligned to the upside, and hourly volatility is elevated, the strategy enters a long breakout trade. Conversely, when probabilities drop below the mean-reversion threshold and the hourly trend is flat, the strategy sells short, targeting pullbacks back into the range. Market orders are used to mirror the immediate execution style of the original expert advisor.

## Risk Management

- A configurable trade delay prevents over-trading by enforcing a pause between consecutive entries.
- `MaxPositions` limits the number of simultaneous open positions. When reversing from an opposite trade the strategy closes the current exposure and opens the new direction in a single market order.
- Dynamic volume estimation uses the account balance, ATR-derived stop distance, and the `RiskPerTrade` percentage to produce a conservative lot size. If the calculation fails, the minimal step volume is used as a safe default.
- Optional safety trades can be enabled for validation or testing environments where at least one trade must appear. The direction of the safety trade follows the combined short- and medium-term trend estimate.
- `StartProtection()` activates StockSharp’s built-in protection block so that unexpected connection issues will not leave positions unmanaged.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `RiskPerTrade` | Risk per trade in percent of the portfolio value (used for dynamic lot calculation). |
| `LookbackPeriod` | Number of finished candles used for moving averages and ATR calculations across all timeframes. |
| `BreakoutThreshold` | Minimum composite probability required for a breakout entry. |
| `MeanReversionThreshold` | Maximum probability that still allows mean-reversion shorts. |
| `TradeDelaySeconds` | Minimum number of seconds between consecutive entries. |
| `MaxPositions` | Maximum simultaneous positions (used for both long and short exposure). |
| `EnableSafetyTrade` | Enables optional validation safety trades when no positions are open. |
| `SafetyTradeIntervalSeconds` | Waiting period between safety trade checks. |
| `CandleType` | Primary timeframe used for the main signal subscription (default: 1 minute). |

## Usage Notes

1. Attach the strategy to an instrument that supports 1-minute data and provides 15-minute and 1-hour candles (StockSharp will aggregate higher frames automatically when the broker supplies minute bars).
2. Set the `Volume` property if a fixed order size is required. Otherwise the strategy derives a conservative size from account balance and ATR.
3. Adjust thresholds and lookback lengths according to the target market’s volatility profile. Higher volatility pairs may benefit from larger thresholds to avoid frequent false breakouts.
4. Safety trades are primarily intended for validation scenarios where the original EA executed at least one trade even without a signal. Disable them for normal live trading environments.

The conversion retains the original idea of mixing breakout detection with reversion safeguards while relying on StockSharp’s high-level indicator framework to remain efficient and test-friendly.
