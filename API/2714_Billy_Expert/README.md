# Billy Expert Pullback Buyer
[Русский](README_ru.md) | [中文](README_cn.md)

## Overview
Billy Expert is a long-only pullback strategy converted from the MetaTrader 5 Expert Advisor "Billy expert". It waits for a sequence of falling highs and opens on the base timeframe, then checks bullish confirmations from two Stochastic oscillators calculated on different higher timeframes. When both oscillators agree that upside momentum is present, the system adds a new long position, up to a configurable limit.

The conversion follows the StockSharp high-level API guidelines. Trade volume, maximum simultaneous entries, protective stops and take profits are controlled through strategy parameters so the behaviour matches the original MQL logic.

## How It Works
1. Subscribe to the primary candle series (default 1 minute) and two higher timeframes for the Stochastic oscillators (defaults 5 and 6 minutes).
2. Track the latest four completed candles on the base timeframe. A valid pullback requires strictly decreasing highs *and* opens across those four bars.
3. Evaluate the fast and slow Stochastic oscillators. The strategy demands that for each oscillator both the latest and the previous values of %K stay above %D, signalling that momentum has already flipped to the upside on both timeframes.
4. If the pullback and momentum filters confirm and the number of open long trades is below `MaxPositions`, send a market buy order with size `TradeVolume`.
5. Optional stop-loss and take-profit levels, expressed in pips, are converted to absolute price distances using the instrument's `PriceStep`. If either distance is set to zero the corresponding protective order is omitted.
6. Positions are closed only via those protective levels, mimicking the original expert advisor behaviour.

## Parameters
- `TradeVolume` – order size for each entry (default `0.01`).
- `StopLossPips` – stop distance in pips (default `0`, disabled).
- `TakeProfitPips` – profit target in pips (default `32`).
- `MaxPositions` – maximum simultaneous long trades (default `6`).
- `Signal Candle` – base timeframe used for price patterns (default `1` minute).
- `Fast Stochastic TF` – timeframe for the fast oscillator (default `5` minutes).
- `Slow Stochastic TF` – timeframe for the slow oscillator (default `6` minutes). Must be longer than the fast timeframe.

## Filters and Behaviour
- **Direction**: Long only.
- **Entry trigger**: Four-bar pullback with both opens and highs decreasing.
- **Momentum filter**: Dual Stochastic oscillators with %K above %D on the current and previous readings.
- **Risk management**: Optional pip-based stop-loss and take-profit. No trailing logic.
- **Position sizing**: Fixed `TradeVolume` per entry, capped by `MaxPositions`.
- **Markets**: Designed for forex pairs quoted with fractional pips, but works with any instrument providing a valid `PriceStep`.

## Usage Notes
- Ensure `Fast Stochastic TF` is strictly shorter than `Slow Stochastic TF`, otherwise the strategy stops on launch.
- Because exits rely solely on protective orders, tune `StopLossPips` and `TakeProfitPips` to the instrument's volatility.
- The strategy ignores bearish signals and does not scale out; use portfolio-level risk controls for additional protection.
- For backtesting, provide enough warm-up candles so both Stochastic oscillators can form before the first trade.
