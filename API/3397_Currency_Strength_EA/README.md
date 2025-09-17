# Currency Strength EA Strategy

## Overview

This strategy is a StockSharp port of the MetaTrader "Currency Strength EA". It tracks a basket of major forex pairs, calculates relative strength values for each currency, and trades the configured symbol when the base currency is strong and the quote currency is weak (or vice versa). The implementation focuses on high-level StockSharp APIs and keeps the logic transparent for further customization.

## Trading Logic

1. Subscribe to the configured basket of currency pairs and the trading symbol.
2. For every finished strength candle, calculate a ratio between the candle close and the recent high/low range. Optionally apply linear or simple smoothing across the lookback window.
3. Convert the ratio into a 0-9 strength score (9 for the strongest readings).
4. Aggregate strength scores per currency by averaging the contributions from all basket pairs.
5. When the base currency strength is above the upper limit and the quote currency strength is below the lower limit, enter a long position. When the reverse condition occurs, enter a short position.
6. Manage protective stops, take profits, and optional trailing stops using ATR- or point-based distances.

The strategy closes or reverses positions when the opposite signal is generated. Trailing stops gradually tighten the protection after favorable price moves.

## Parameters

| Name | Description | Default |
| --- | --- | --- |
| `CandleType` | Candle type for trading signals. | 30-minute time-frame |
| `StrengthCandleType` | Candle type for strength calculations. | 4-hour time-frame |
| `CurrencyPairs` | Comma-separated list of pairs in the strength basket. | Major and cross pairs |
| `NumberOfCandles` | Lookback window for range calculation. | 55 |
| `ApplySmoothing` | Enables smoothing of ratio values. | `true` |
| `TriangularWeighting` | Uses linear weighting when smoothing is enabled. | `true` |
| `UpperLimit` | Threshold for a strong currency. | `7.2` |
| `LowerLimit` | Threshold for a weak currency. | `3.3` |
| `AtrPeriod` | ATR lookback for risk management. | `14` |
| `StopMode` | Defines whether risk distances are ATR- or point-based. | `InAtr` |
| `StopLossFactor` | Stop loss distance multiplier. Set `0` to disable. | `0` |
| `TakeProfitFactor` | Take profit distance multiplier. Set `0` to disable. | `1` |
| `TrailingStop` | Trailing stop distance in points. Set `0` to disable. | `30` |
| `TrailingStep` | Minimum move before updating the trailing stop in points. | `5` |
| `StartTime` / `EndTime` | Trading session window (HH:mm). | `10:00` / `16:00` |
| `TimeMode` | Clock used for the session window (server, GMT, or local). | `Server` |
| `BaseCurrency` | Base currency code of the traded symbol. | `EUR` |
| `QuoteCurrency` | Quote currency code of the traded symbol. | `USD` |

## Risk Management

- Stop loss and take profit levels are calculated using ATR or price steps based on the selected mode.
- Trailing stops can be configured independently from the static stop loss.
- If no stop or target distances are provided, the strategy relies on reverse signals to exit positions.

## Notes and Limitations

- The strategy trades only the configured `Security`. It does not open positions on the entire basket. To trade multiple symbols, run multiple instances.
- Money-management and martingale features from the original EA are intentionally omitted for clarity.
- Smoothing waits until the chosen moving average is formed before producing strength values.
- Ensure that all basket securities are available in the connection; missing symbols will be reported in the log.

## Usage Tips

1. Configure the trading symbol (`Security`) and portfolio before starting the strategy.
2. Adjust the basket to match the broker's naming conventions if prefixes or suffixes are required.
3. Tune the strength thresholds and ATR parameters during backtesting to match the desired aggressiveness.
