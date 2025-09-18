# Sidus v1 Strategy

## Overview
Sidus v1 is a trend-following strategy that combines two sets of Exponential Moving Averages (EMAs) with Relative Strength Index (RSI) filters. The original MetaTrader 4 expert advisor opens a position when a fast EMA diverges from a slower EMA and the RSI confirms either oversold or overbought conditions. This StockSharp port keeps the core logic, limiting trades to candles with low volume and attaching asymmetric protective orders for long and short positions.

## Indicators Used
- **Fast EMA (buy leg)** – measures short-term momentum for long entries.
- **Slow EMA (buy leg)** – represents the longer-term trend filter for long entries.
- **Fast EMA (sell leg)** – measures short-term momentum for short entries.
- **Slow EMA (sell leg)** – represents the longer-term trend filter for short entries.
- **RSI (buy leg)** – validates oversold conditions for long trades.
- **RSI (sell leg)** – validates overbought conditions for short trades.

## Trading Logic
1. Subscribe to the configured candle series (default 15-minute time frame).
2. Compute all EMA and RSI indicators on each finished candle.
3. Skip signal evaluation when the candle volume exceeds the configured limit (default 10).
4. **Buy condition**:
   - Fast EMA minus slow EMA is below the buy threshold.
   - RSI value is below the buy RSI threshold.
   - No existing long exposure (net position must be non-positive).
5. **Sell condition**:
   - Fast EMA (sell leg) minus slow EMA (sell leg) is above the sell threshold.
   - RSI (sell leg) is above the sell RSI threshold.
   - No existing short exposure (net position must be non-negative).
6. When a signal triggers, cancel any pending protective orders, execute a market order sized to flip the net position to the desired side, and immediately place take-profit and stop-loss orders tailored to the position direction.

## Risk Management
- Long trades place a take-profit at `entry + BuyTakeProfitPips * priceStep` and a stop-loss at `entry - BuyStopLossPips * priceStep`.
- Short trades place a take-profit at `entry - SellTakeProfitPips * priceStep` and a stop-loss at `entry + SellStopLossPips * priceStep`.
- Protective orders reuse the current security price step; change the pip parameters to adapt to instruments with different tick sizes.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `FastEmaLength` | Length of the fast EMA for buy signals. | 23 |
| `SlowEmaLength` | Length of the slow EMA for buy signals. | 62 |
| `FastEma2Length` | Length of the fast EMA for sell signals. | 18 |
| `SlowEma2Length` | Length of the slow EMA for sell signals. | 54 |
| `RsiPeriod` | RSI period for buy confirmation. | 67 |
| `RsiPeriod2` | RSI period for sell confirmation. | 97 |
| `BuyDifferenceThreshold` | Maximum fast-slow EMA difference to allow buys. | 63 |
| `BuyRsiThreshold` | Maximum RSI level to allow buys. | 59 |
| `SellDifferenceThreshold` | Minimum fast-slow EMA difference to allow sells. | -57 |
| `SellRsiThreshold` | Minimum RSI level to allow sells. | 60 |
| `BuyTakeProfitPips` | Take-profit distance (pips) for long trades. | 95 |
| `BuyStopLossPips` | Stop-loss distance (pips) for long trades. | 100 |
| `SellTakeProfitPips` | Take-profit distance (pips) for short trades. | 17 |
| `SellStopLossPips` | Stop-loss distance (pips) for short trades. | 69 |
| `OrderVolume` | Volume for newly opened positions. | 0.5 |
| `MaxCandleVolume` | Maximum candle volume allowed to trade. | 10 |
| `CandleType` | Time frame used for calculations. | 15-minute candles |

## Usage Notes
- Ensure that the connected security supports simultaneous market, stop, and limit orders for proper risk management.
- Adjust pip settings to reflect the instrument’s tick size if it differs from the MT4 point value assumed by the original expert.
- The strategy operates on net positions; it will flatten opposing exposure before establishing a new trade in the opposite direction.
