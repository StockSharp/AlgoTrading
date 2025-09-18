# RM Stochastic Band Strategy

## Overview

The **RM Stochastic Band Strategy** is a high-level StockSharp port of the MetaTrader expert advisor *EA RM Stochastic Band* by Ronny Maheza. The strategy observes three stochastic oscillators calculated on different timeframes (base, mid, and high) and opens trades only when all three confirm oversold or overbought conditions. Upon entry, exit levels are derived from the Average True Range (ATR) measured on the higher timeframe, replicating the ATR-based stop-loss and take-profit levels in the original expert advisor. Additional execution filters include a configurable minimum portfolio value as a margin proxy and a spread control that adapts its tolerance depending on the observed spread.

## Core Logic

1. **Multi-timeframe stochastic confirmation**  
   - Primary execution timeframe (default M1) generates the trading signal.  
   - Confirmation timeframes (default M5 and M15) must agree with the signal direction.  
   - A trade is opened only if the stochastic %K values on all three timeframes are simultaneously below the oversold level (long setup) or above the overbought level (short setup).

2. **Volatility-based exits with ATR**  
   - ATR is calculated on the highest timeframe (default M15).  
   - Stop-loss = `entry price ± ATR * StopLossMultiplier`.  
   - Take-profit = `entry price ± ATR * TakeProfitMultiplier`.  
   - Prices are monitored on the base timeframe candles; if a candle touches either level the position is closed at market.

3. **Execution and safety filters**  
   - Orders are skipped when the observed spread (BestAsk - BestBid) exceeds the adaptive threshold. If the spread is higher than the standard limit, the looser cent-account limit is applied, mirroring the source EA logic.  
   - Trading is blocked while the portfolio value is below `MinMargin`.  
   - Only one position can be open at a time, and no new trade is initiated if active orders exist.

## Indicators and Subscriptions

| Indicator | Timeframe | Purpose |
|-----------|-----------|---------|
| Stochastic Oscillator | Base timeframe (default 1 minute) | Generates primary signal (%K only is used). |
| Stochastic Oscillator | Mid timeframe (default 5 minutes) | Confirms the primary signal direction. |
| Stochastic Oscillator | High timeframe (default 15 minutes) | Provides long-term confirmation. |
| Average True Range | High timeframe (default 15 minutes) | Defines volatility-adjusted stop-loss and take-profit distances. |

Level-1 data is subscribed to capture the best bid and ask for spread evaluation.

## Entry Rules

- **Long setup**: All three stochastic %K values are below `OversoldLevel`. When triggered, the strategy buys at market volume `OrderVolume` and stores ATR-based exit levels.
- **Short setup**: All three stochastic %K values are above `OverboughtLevel`. A market sell is executed with the same volume handling.

## Exit Rules

- **Stop-loss**: For long positions, exit when the candle low touches `entry - ATR * StopLossMultiplier`. For short positions, exit when the candle high reaches `entry + ATR * StopLossMultiplier`.
- **Take-profit**: For long positions, exit when the candle high touches `entry + ATR * TakeProfitMultiplier`. For short positions, exit when the candle low reaches `entry - ATR * TakeProfitMultiplier`.
- After an exit the internal stop and target placeholders are cleared so that the next signal can recalculate fresh levels.

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `OrderVolume` | Volume of each market order. | 0.1 |
| `StochasticLength` | %K lookback period. | 5 |
| `StochasticSmoothing` | Smoothing applied to %K. | 3 |
| `StochasticSignalLength` | %D length. | 3 |
| `AtrPeriod` | ATR period on the high timeframe. | 14 |
| `StopLossMultiplier` | ATR multiplier for the stop-loss. | 1.5 |
| `TakeProfitMultiplier` | ATR multiplier for the take-profit. | 3.0 |
| `MinMargin` | Minimum portfolio value required for trading. | 100 |
| `MaxSpreadStandard` | Spread cap for standard accounts. | 3 |
| `MaxSpreadCent` | Spread cap used when the current spread already exceeds the standard cap. | 10 |
| `OversoldLevel` | Oversold threshold for stochastic %K. | 20 |
| `OverboughtLevel` | Overbought threshold for stochastic %K. | 80 |
| `BaseCandleType` | Primary timeframe (default 1-minute candles). | 1-minute | 
| `MidCandleType` | Confirmation timeframe (default 5-minute candles). | 5-minute |
| `HighCandleType` | Confirmation + ATR timeframe (default 15-minute candles). | 15-minute |

All parameters support optimization ranges identical to the MetaTrader inputs where appropriate.

## Implementation Notes

- The strategy uses `SubscribeCandles(...).BindEx(...)` to obtain indicator values strictly through the high-level API as mandated by the project guidelines.  
- Spread is computed from live Level-1 updates; without bid/ask data, trading remains disabled, ensuring safe operation on data feeds that do not provide quotes.  
- Positions are managed purely through market orders, mirroring the original EA that relied on market entries with pre-calculated stop-loss and take-profit levels.  
- There is no breakeven or trailing logic because the MQL source did not implement those features despite having related input parameters.

## Usage Tips

1. Attach the strategy to the desired security and ensure that Level-1 (bid/ask) data is available for proper spread filtering.  
2. Tune the stochastic thresholds and ATR multipliers to match the target instrument's volatility profile.  
3. When optimizing, consider testing different timeframe combinations if the market you trade has different dominant cycles than the original M1/M5/M15 structure.
