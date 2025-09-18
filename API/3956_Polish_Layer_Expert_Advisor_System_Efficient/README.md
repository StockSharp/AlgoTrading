# Polish Layer Expert Advisor System Efficient

## Overview
This strategy is a direct port of the MQL4 expert advisor "Polish Layer Expert Advisor System Efficient". It is designed for intraday charts (the original author recommended 5 or 15 minute candles) and restricts trading to a single position at a time. Trend direction is defined by the alignment between a fast and a slow price moving average together with two smoothed RSI filters. Actual entries require a triple confirmation from the Stochastic Oscillator, DeMarker, and Williams %R indicators in order to capture reversals from extreme conditions that occur within the prevailing trend.

## Trading logic
1. **Trend filter.** A 9-period simple moving average (SMA) of close prices must be above the 45-period linear weighted moving average (LWMA) to allow longs and below it to allow shorts. At the same time, the 9-period SMA of RSI must be above (for longs) or below (for shorts) the 45-period SMA of RSI. Any disagreement between the price and RSI filters blocks new orders.
2. **Stochastic trigger.** When the trend filter is bullish, the strategy waits for the Stochastic %K line to cross upward above the oversold threshold (default 19) and simultaneously cross above %D. For bearish setups, %K must cross downward below the overbought threshold (default 81) and drop under %D. The slowing factor is preserved from the MQL4 script.
3. **Momentum confirmations.** A long signal additionally requires DeMarker to cross upward through 0.35 and Williams %R to cross upward through −81 on the current completed candle. Short signals demand downward crossings through 0.63 and −19 respectively. All crossings are evaluated between the previous finished candle and the current one.
4. **Position management.** Only market orders are issued and the strategy remains flat until a protective stop or target closes the trade. Protective levels are recalculated from pip-based parameters using the instrument price step. If the price step is not available the protection is disabled.

## Risk management
* **Stop-loss / take-profit.** Distances are configured in pips. When positive, the values are converted to actual price offsets using `Security.PriceStep` (1 pip = 1 price step) and applied immediately after entry. Setting a parameter to `0` disables the corresponding protective level.
* **Single position.** The original EA never pyramided, therefore the port refuses to enter if a position already exists.

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `Volume` | `0.1` | Order volume in lots. Adjust according to the broker contract size. |
| `CandleType` | `TimeSpan.FromMinutes(15).TimeFrame()` | Candle type used for indicator calculations. Set to a 5 or 15 minute timeframe to mirror the original EA. |
| `RsiPeriod` | `14` | Lookback length for the base RSI calculation. |
| `ShortPricePeriod` | `9` | Period of the fast price SMA used in the trend filter. |
| `LongPricePeriod` | `45` | Period of the slow price LWMA used in the trend filter. |
| `ShortRsiPeriod` | `9` | Length of the fast SMA applied to RSI values. |
| `LongRsiPeriod` | `45` | Length of the slow SMA applied to RSI values. |
| `StochasticKPeriod` | `5` | Base %K period for the Stochastic Oscillator. |
| `StochasticDPeriod` | `3` | Smoothing period for the %D line. |
| `StochasticSlowing` | `3` | Additional smoothing factor applied to %K. |
| `DemarkerPeriod` | `14` | Averaging period for the DeMarker indicator. |
| `WilliamsPeriod` | `14` | Lookback period for Williams %R. |
| `StochasticOversoldLevel` | `19` | Oversold threshold that %K must cross upward to enable long entries. |
| `StochasticOverboughtLevel` | `81` | Overbought threshold that %K must cross downward to enable short entries. |
| `DemarkerBuyLevel` | `0.35` | Minimum DeMarker value required for long entries (crossing from below). |
| `DemarkerSellLevel` | `0.63` | Maximum DeMarker value permitted for short entries (crossing from above). |
| `WilliamsBuyLevel` | `-81` | Williams %R crossing level confirming long entries. |
| `WilliamsSellLevel` | `-19` | Williams %R crossing level confirming short entries. |
| `StopLossPips` | `7777` | Stop-loss distance in pips. The very large default effectively disables the stop unless configured. |
| `TakeProfitPips` | `17` | Take-profit distance in pips. Set to `0` to disable the fixed target. |

## Notes
* Ensure that `Security.PriceStep`, `Security.MinVolume`, and `Security.VolumeStep` are properly configured; the strategy assumes one pip equals one price step when converting risk parameters.
* The entry filters depend on indicator crossovers between consecutive completed candles. When importing historical data, keep bar alignment identical to the original timeframe to reproduce results.
