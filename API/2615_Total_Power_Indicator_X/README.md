# Total Power Indicator X Strategy

## Overview
The strategy recreates the behaviour of the MetaTrader expert "Exp_TotalPowerIndicatorX" using StockSharp high level APIs. It relies on a custom implementation of the Total Power Indicator that measures the dominance of bulls and bears by counting how many candles in a rolling window close above or below an internal EMA baseline. Trading decisions are made when the bullish and bearish strength lines cross each other.

The indicator works on any symbol and timeframe. By default the strategy subscribes to 4-hour candles, matching the original expert advisor configuration, but the timeframe can be adjusted through a parameter.

## Trading Logic
1. For every finished candle the strategy feeds the Total Power Indicator with the candle data. The indicator:
   - Calculates an EMA with period **Power Period**.
   - Counts how many candles within **Lookback Period** had `High > EMA` (bulls) and `Low < EMA` (bears).
   - Converts the counts into percentage style strength values in the 0–100 range.
2. A **bullish crossover** (bull strength rising above bear strength) triggers a long entry when long trading is enabled and there are no open positions.
3. A **bearish crossover** (bear strength rising above bull strength) triggers a short entry when short trading is enabled and there are no open positions.
4. Opposite crossovers close existing positions when the relevant exit switches are enabled.
5. An optional trading session filter forces all positions to be closed outside the configured time window and disables new entries during that period.
6. Optional stop-loss and take-profit levels are expressed in multiples of the security price step. They are recalculated after each entry and exit as soon as the candle’s high or low breaches the level.

## Parameters
- **Candle Type** – timeframe used for indicator calculations. Default: 4-hour candles.
- **Power Period** – EMA length inside the indicator; mirrors the MQL input. Default: 10.
- **Lookback** – number of candles used to count bullish and bearish dominance. Default: 45.
- **Volume** – order size sent to the exchange or simulator. Default: 1.
- **Enable Long Entry / Enable Short Entry** – allow or forbid new positions in the corresponding direction.
- **Enable Long Exit / Enable Short Exit** – close positions on opposite signals. Disable to keep positions open until manually closed or stopped out.
- **Use Trading Hours** – enable the time filter. When active the strategy trades only between **Start Hour/Minute** and **End Hour/Minute** and closes any open positions outside that interval. Overnight windows (start later than end) are supported.
- **Stop Loss Points / Take Profit Points** – distances from the entry price measured in price steps. Set to zero to disable the level. The calculation uses `Security.PriceStep`, therefore make sure the security metadata is available.

## Notes
- The strategy opens a new position only when no existing position is active on the security, emulating the behaviour of the original expert.
- Because stop-loss and take-profit calculations depend on the instrument’s price step, running the strategy without that metadata keeps the protective levels disabled automatically.
- The indicator value is plotted on the chart area when the UI is available, which helps to visualise the crossings between bull and bear strength.
