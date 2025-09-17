# 555 Scalper Strategy

## Overview
The 555 Scalper strategy is a direct port of the "555 Scalper" MetaTrader expert advisor. It operates on any primary timeframe while relying on higher timeframe filters and monthly momentum confirmation. The algorithm combines a fast/slow linear weighted moving average crossover with a higher timeframe momentum confirmation and a monthly MACD filter. Protective logic mirrors the original EA, including break-even movements, classic pip-based trailing, equity-based emergency stops, and money-based exits.

## Trading Logic
- **Trend Filter:** Calculates a fast and a slow LWMA on the typical price of the trading timeframe. Longs require the fast LWMA to stay above the slow LWMA; shorts require the opposite.
- **Candle Structure:** Validates that the previous two completed candles overlap (low two bars ago below the prior high for longs, and vice versa for shorts) to approximate the fractal-style confirmation used by the EA.
- **Momentum Filter:** Uses a 14-period Momentum indicator calculated on a higher timeframe derived from the trading timeframe (e.g., M1 → M15, M5 → M30, M15 → H1, etc.). A trade becomes valid only if at least one of the last three momentum readings deviates from the neutral 100 level by the configured threshold (0.3 by default).
- **MACD Confirmation:** Applies a monthly MACD (12/26/9) filter and only buys when the MACD main line is above the signal line, or sells when it is below.
- **Position Sizing:** Starts from a base lot and multiplies each additional entry by the configured lot exponent, enabling controlled pyramiding up to the maximum number of trades per direction.

## Risk Management
- **Initial Stop-Loss and Take-Profit:** Each new position receives an initial stop-loss and take-profit based on MetaTrader-style pip distances.
- **Break-Even Move:** When price travels a configurable number of pips in profit, the stop is moved to break-even plus an offset.
- **Trailing Stop:** Implements the original pip trailing logic by shifting the stop with price once the trade runs in profit.
- **Money Targets:** Optional money and percentage take-profits close the position once floating profit reaches the configured thresholds.
- **Money Trailing:** Tracks peak floating profit and exits if profit retraces by a configurable amount after reaching the trigger level.
- **Equity Stop:** Monitors the maximum account equity achieved during the session and liquidates all positions if floating drawdown exceeds the allowed percentage.

## Parameters
- **BaseVolume / LotExponent:** Define the initial trade size and the multiplier for additional entries.
- **StopLossSteps / TakeProfitSteps:** Pip distances for protective levels.
- **FastMaPeriod / SlowMaPeriod:** Periods of the fast and slow LWMA trend filter.
- **Momentum thresholds:** Required deviation from 100 for long and short setups.
- **MaxTrades:** Maximum number of layered entries per direction.
- **BreakEven and Trailing settings:** Configure the pip-based break-even trigger, offset, and trailing distance.
- **Money management:** Enable or disable money take-profit, percent take-profit, and money trailing controls.
- **Equity stop:** Percentage drawdown from the equity peak that triggers a global exit.

## Usage Notes
1. Attach the strategy to any instrument and select the desired trading timeframe through the `CandleType` parameter.
2. The higher timeframe momentum feed is calculated automatically based on the primary timeframe; ensure that historical data for both timeframes is available.
3. The monthly MACD feed requires monthly candle data. When testing, provide sufficient history to warm up the MACD signal.
4. Adjust volume, pip distances, and money-management thresholds according to the instrument's volatility and the account's risk profile.

The strategy reproduces the core decision process of the original EA while leveraging StockSharp's high-level API for data subscriptions, indicator management, and order execution.
