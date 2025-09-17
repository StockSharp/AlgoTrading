# Morning/Evening Star CCI Strategy

## Overview
This strategy replicates the MetaTrader 5 Expert Advisor **Expert_AMS_ES_CCI** using the StockSharp high-level API. It scans for Morning Star and Evening Star three-candle reversal patterns and requires confirmation from the Commodity Channel Index (CCI) before opening new positions. The logic works with finished candles only and operates on the primary security specified in the strategy settings.

## Trading Logic
- **Morning Star long entry**
  - Detect three consecutive candles that form a Morning Star pattern:
    - Candle 1: strong bearish body (body size larger than the average body over the selected window).
    - Candle 2: small-bodied candle that gaps lower than Candle 1.
    - Candle 3: closes above the midpoint of Candle 1.
  - Confirm that the CCI value on the signal bar is less than the negative entry threshold (default −50).
- **Evening Star short entry**
  - Detect a valid Evening Star pattern:
    - Candle 1: strong bullish body.
    - Candle 2: small-bodied candle that gaps above Candle 1.
    - Candle 3: closes below the midpoint of Candle 1.
  - Confirm that the CCI value on the signal bar is greater than the positive entry threshold (default +50).
- **Position exit rules**
  - Close short positions when CCI crosses back above −NeutralThreshold or falls below +NeutralThreshold (default ±80).
  - Close long positions when CCI crosses back below +NeutralThreshold or falls beneath −NeutralThreshold.
  - No additional stop-loss or take-profit rules are embedded; users can add external protections if required.

## Indicators
- **Commodity Channel Index (CCI)** – confirmation filter, default period 25.
- **Simple Moving Average of candle bodies** – calculates the average body size over the last *BodyAveragePeriod* candles (default 5) to validate pattern strength.

## Parameters
| Name | Description | Default | Notes |
| --- | --- | --- | --- |
| `CciPeriod` | Number of bars used in the CCI calculation. | 25 | Optimizable. |
| `BodyAveragePeriod` | Number of candles used to measure the average body size. | 5 | Optimizable. |
| `EntryThreshold` | Absolute CCI value required for new trades. | 50 | Positive value; the strategy checks ±EntryThreshold. |
| `NeutralThreshold` | Absolute CCI level that defines the exit zone. | 80 | Positive value; the strategy checks ±NeutralThreshold. |
| `CandleType` | Candle type (timeframe) used for analysis. | 1 hour time frame | Change to match the desired resolution. |

## Notes
- The strategy subscribes to candle updates via `SubscribeCandles` and uses `Bind` to receive indicator values.
- Trades are executed with market orders using `BuyMarket` and `SellMarket`.
- All comments in the code are written in English as required.
- To extend risk management, combine the strategy with `StartProtection` or custom money-management modules.
