# SVOS EURJPY D1 Strategy

## Overview
This strategy is a C# conversion of the MetaTrader 4 expert advisor **SVOS_EURJPY_D1**. It operates on daily candles for EURJPY and
combines a regime classifier with pattern recognition and indicator filters. The Vertical Horizontal Filter (VHF) distinguishes
between trending and ranging market states. When the market is trending the strategy relies on the MACD histogram (OSMA) slope,
while in ranging conditions it falls back to the Stochastic oscillator. Candlestick patterns such as engulfing bars and
morning/evening stars are used to close positions aggressively against unfavourable price action.

## Trading logic
- **Regime detection** – the previous day's VHF value is compared with `VhfThreshold`. Values above the threshold activate the
  trend-following block, otherwise the range block is used.
- **Trend confirmation** – two EMAs (5 and 20 periods) are compared with a slow EMA (130 periods, matching the six-month filter of
the original EA) to scale position sizes. In up-trends buy volume is multiplied by `RiskBoost`; in down-trends sell volume is
  multiplied.
- **Indicator filters**:
  - Trend regime: go long when OSMA is positive and rising (`OSMA[1] > 0` and `OSMA[1] > OSMA[2]`). Go short when OSMA is negative
    and falling.
  - Range regime: go long when the Stochastic main line crosses above its signal, go short when it crosses below.
  - Volatility guard: the previous standard deviation must exceed `StdDevMinimum` before any signal is accepted.
- **Price action filters** – the most recent completed candle must not form a doji (`DojiDivisor` ratio) and must confirm the
  direction (bullish for longs, bearish for shorts). Opposite engulfing or star patterns trigger immediate liquidation of the
  respective side.
- **Position limits** – the total number of open orders is capped by `MaxTrendOrders` in trending markets and by `MaxRangeOrders`
  in ranging markets.
- **Risk management** – every order carries fixed stop-loss and take-profit levels (`StopLossPips`, `TakeProfitPips`). A trailing
  stop is activated when the floating profit exceeds `TrailingStopPips`; it is recalculated using the candle extremes to mimic the
  MetaTrader behaviour.

## Indicator usage
- **Exponential Moving Average (5, 20, 130)** – used for direction confirmation and volume scaling.
- **Vertical Horizontal Filter** – custom indicator that measures the ratio between net movement and cumulative close-to-close
  changes to detect trends versus ranges.
- **MACD (OSMA)** – the difference between MACD and its signal line drives trending entries and exits.
- **Stochastic Oscillator** – %K and %D values provide mean-reversion signals for ranging markets.
- **Standard Deviation** – ensures volatility is high enough before allowing new trades.

## Order management
- Orders are executed with `BuyMarket`/`SellMarket` and stored internally so that individual stops and targets can be simulated in
  StockSharp's netting environment.
- When stop-loss or take-profit levels are touched within the candle range, the corresponding portion of the position is closed.
- The trailing stop follows the candle high (for longs) or low (for shorts) while maintaining the configured distance.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `LotSize` | Base order size expressed in lots. | `0.1` |
| `RiskBoost` | Multiplier applied to the lot size when the EMA trend filter is aligned. | `3` |
| `TakeProfitPips` | Take-profit distance in pips. | `350` |
| `StopLossPips` | Stop-loss distance in pips. | `90` |
| `TrailingStopPips` | Trailing-stop distance in pips (always active). | `150` |
| `StochKPeriod` | %K length of the Stochastic oscillator. | `8` |
| `StochDPeriod` | %D length of the Stochastic oscillator. | `3` |
| `StochSlowing` | Smoothing factor applied to %K. | `3` |
| `StdDevPeriod` | Lookback window for the standard deviation filter. | `20` |
| `StdDevMinimum` | Minimal standard deviation required before new trades can open. | `0.3` |
| `VhfPeriod` | Length of the Vertical Horizontal Filter. | `20` |
| `VhfThreshold` | Regime threshold; higher values denote trending markets. | `0.4` |
| `MaxTrendOrders` | Maximum number of simultaneously open orders during trends. | `4` |
| `MaxRangeOrders` | Maximum number of simultaneously open orders during ranges. | `2` |
| `MacdFastLength` | Fast EMA length inside MACD. | `10` |
| `MacdSlowLength` | Slow EMA length inside MACD. | `25` |
| `MacdSignalLength` | Signal EMA length for MACD. | `5` |
| `DojiDivisor` | Ratio used to flag doji candles (body smaller than range / divisor). | `8.5` |
| `CandleType` | Candle type used for analysis (daily by default). | `1 day` |
| `PipSizeOverride` | Optional pip-size override; `0` enables automatic detection from `Security.PriceStep`. | `0` |

## Implementation notes
- The original EA referenced a six-month EMA from a monthly timeframe. The port computes a 130-period EMA on daily closes to
  reproduce the same smoothing while keeping a single data subscription.
- Stops, targets and trailing logic are reproduced inside the strategy because StockSharp nets positions by default. Each entry is
  tracked individually to honour the MetaTrader behaviour.
- Trailing stop updates use candle highs/lows to approximate intraday price movements. Results may differ slightly from tick-based
  trailing in MetaTrader when large intraday reversals occur.
- Pip size is calculated from `Security.PriceStep`; use `PipSizeOverride` if the broker uses a non-standard step for JPY pairs.

## Usage
1. Attach the strategy to EURJPY daily data or update `CandleType` if another timeframe is desired.
2. Verify that the pip size is detected correctly; adjust `PipSizeOverride` if necessary.
3. Configure money-management parameters (`LotSize`, `RiskBoost`) to match account constraints.
4. Run the strategy in the StockSharp Designer or API Runner to validate behaviour before trading live.
