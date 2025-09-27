# Reverse Strategy

## Overview

Reverse Strategy is a mean-reversion trading system that combines Bollinger Bands and the Relative Strength Index (RSI) to identify exhausted moves. The strategy looks for price reversals near the Bollinger envelopes while simultaneously requiring the RSI to cross back from an oversold or overbought zone. Once both conditions are satisfied, the strategy enters against the previous move and manages trades with fixed band-based stops and targets.

## Trading Logic

1. Subscribe to the configured candle series (default 5-minute candles).
2. Calculate Bollinger Bands using a simple moving average with the configured period and deviation multiplier.
3. Calculate RSI using the configured lookback period.
4. Track the previous finished candle to detect crossovers:
   - **Long setup**: the previous close is below the previous lower band and RSI is below the oversold threshold. The current close must move back above the lower band while RSI rises above the oversold level.
   - **Short setup**: the previous close is above the previous upper band and RSI is above the overbought threshold. The current close must fall back below the upper band while RSI drops below the overbought level.
5. When a long setup triggers, buy at market, set a protective stop one standard deviation below the entry close, and a take profit two standard deviations above it.
6. When a short setup triggers, sell at market, set a protective stop one standard deviation above the entry close, and a take profit two standard deviations below it.
7. Manage open positions:
   - Close long trades if price touches the upper band, hits the stop, or reaches the take-profit target.
   - Close short trades if price touches the lower band, hits the stop, or reaches the take-profit target.

## Parameters

| Name | Description | Default |
| --- | --- | --- |
| `CandleType` | Time frame for the candle subscription. | 5-minute time frame |
| `BollingerPeriod` | Number of bars used for the Bollinger moving average and standard deviation. | 20 |
| `BollingerWidth` | Standard deviation multiplier applied to Bollinger Bands. | 2.0 |
| `RsiPeriod` | Number of bars used to calculate the RSI. | 14 |
| `RsiOverbought` | RSI threshold signaling overbought conditions for short entries. | 70 |
| `RsiOversold` | RSI threshold signaling oversold conditions for long entries. | 30 |

All parameters support optimization via the StockSharp Designer or Runner. Adjusting the oversold/overbought levels changes how aggressive the reversal detection is, while the Bollinger width controls how far price must stretch before signals are considered.

## Usage Notes

- The strategy uses the high-level StockSharp API with automatic candle subscriptions and indicator binding.
- All trading operations rely on market orders (`BuyMarket`/`SellMarket`). Stop-loss and take-profit levels are handled in code rather than as pending orders.
- The default configuration targets major reversals on intraday charts but can be adapted to higher time frames by changing `CandleType`.
- Consider combining the strategy with additional filters (trend, volatility, session time) when running in live environments.
