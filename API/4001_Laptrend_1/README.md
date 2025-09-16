# Laptrend_1 Strategy

## Overview
Laptrend_1 reproduces the logic of the MetaTrader expert advisor **Laptrend_1.mq4**. The strategy blends a multi-timeframe LabTrend channel filter, Fisher Transform momentum confirmation and an ADX trend strength check on 15-minute candles. Orders are opened only when the higher-timeframe (H1) and signal timeframe (M15) LabTrend directions agree, the Fisher transform confirms the move and the ADX shows a strengthening trend. Positions are closed when the momentum reverses, the LabTrend direction changes, or the market transitions into a flat regime where ADX and the DI components converge.

## Trading Logic
- **Primary data** – 15-minute candles drive entries/exits while 1-hour candles feed the long-term LabTrend filter.
- **LabTrend channel** – The code recreates the `LabTrend1_v2.1` indicator by building Donchian-style channels over the last `ChannelLength` bars and narrowing them with the `RiskFactor`. A close above the upper band marks a bullish trend; a close below the lower band marks a bearish trend. The M15 and H1 trends must align to open trades.
- **Fisher Transform** – A custom Fisher Transform (`Fisher_Yur4ik`) tracks momentum on the M15 timeframe. Crosses through zero flip the bullish/bearish bias, while traversing ±0.25 produces exit signals.
- **ADX filter** – The 15-minute Average Directional Index must rise and the dominant DI component has to agree with the proposed trade. When ADX, +DI and –DI fall within `Delta` points of each other, the strategy treats the market as flat, resets the momentum flags and liquidates open positions.
- **Position management** – New positions close any opposite exposure and trade a configurable volume. Exits are triggered by LabTrend reversals, Fisher exits or a flat market condition.

## Risk Management
- **Stop Loss / Take Profit** – Configurable in instrument points (MetaTrader “pips”). They are evaluated against candle highs/lows to mimic protective orders from the original EA.
- **Trailing Stop** – Once the price moves in the trade’s favour, a trailing stop tracks the close at a distance equal to `TrailingStopPoints`. Crossing the trailing level triggers an immediate market exit.
- **Volume** – All orders use the fixed `Volume` parameter (lots).

## Parameters
- `Volume` – Order size in lots. Default 1.
- `AdxPeriod` – ADX smoothing period. Default 14.
- `FisherLength` – Window for the Fisher transform. Default 10.
- `ChannelLength` – Bars used for the LabTrend channel. Default 9.
- `RiskFactor` – LabTrend channel narrowing factor (original indicator range 1..10). Default 3.
- `Delta` – Maximum difference between ADX and DI values before the market is labelled flat. Default 7.
- `StopLossPoints` – Stop loss distance in points. Default 100.
- `TakeProfitPoints` – Take profit distance in points. Default 40.
- `TrailingStopPoints` – Trailing stop distance in points. Default 100.
- `SignalCandleType` – Candle series for signal calculations (default M15).
- `TrendCandleType` – Candle series for the higher-timeframe LabTrend filter (default H1).

## Notes
- The original MQL implementation worked on every incoming tick; this port processes completed M15 candles, which keeps the logic deterministic while still respecting the indicator calculations.
- Stop loss, take profit and trailing exits are executed with market orders when the candle’s high/low breaches the configured thresholds. This mirrors the behaviour of MetaTrader protective orders without maintaining explicit stop/limit orders.
- Ensure that the data source supplies both the 15-minute and 1-hour candle series defined in the parameters before starting the strategy.
