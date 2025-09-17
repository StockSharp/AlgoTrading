# EurGbp EA Strategy

## Overview
The EurGbp EA strategy mirrors the original MetaTrader expert advisor by comparing hourly MACD momentum of EUR/USD and GBP/USD while placing trades on the configured primary instrument (typically EUR/GBP). The approach exploits relative strength between euro and pound majors to anticipate moves in the cross pair.

## Indicators
* **MACD (12, 26, 9)** on EUR/USD (signal and histogram).
* **MACD (12, 26, 9)** on GBP/USD (signal and histogram).

Both indicators are evaluated on the same timeframe selected through the `Candle Type` parameter (default is 1 hour).

## Trading Logic
1. Subscribe to candles for the trading security plus EUR/USD and GBP/USD.
2. Compute MACD signal and histogram for both reference pairs.
3. **Buy condition:**
   * EUR/USD histogram &lt; GBP/USD histogram, **and**
   * EUR/USD signal &gt; GBP/USD signal,
   * No existing long position (or an existing short that will be flattened).
4. **Sell condition:**
   * GBP/USD histogram &lt; EUR/USD histogram, **and**
   * GBP/USD signal &gt; EUR/USD signal,
   * No existing short position (or an existing long that will be flattened).
5. Only one trade per bar in each direction is allowed to avoid duplicate entries.
6. Stop-loss and take-profit orders are attached immediately after entry using the configured point distances.

## Parameters
| Name | Description | Default |
| ---- | ----------- | ------- |
| Candle Type | Timeframe for all candle subscriptions. | 1 hour |
| EURUSD Security | Instrument providing EUR/USD candles. | Must be set |
| GBPUSD Security | Instrument providing GBP/USD candles. | Must be set |
| Volume | Order volume (lots). | 0.01 |
| Stop Loss | Protective stop in price steps. | 75 |
| Take Profit | Profit target in price steps. | 46 |

## Risk Management
* `Stop Loss` and `Take Profit` are measured in price steps of the traded security. Ensure the security has a valid `PriceStep` value.
* Protection starts automatically when the strategy launches (`StartProtection`).
* If either distance is zero, the respective protective order is skipped.

## Usage Notes
* Assign the main trading security to the strategy instance before start (for example, EUR/GBP).
* Configure `EURUSD Security` and `GBPUSD Security` to reference available data sources within your connection.
* The strategy requires synchronized data for all three securities on the selected timeframe to generate signals reliably.
* Only market orders are used. Existing opposite positions are closed by sending the inverse order volume.

## Conversion Notes
* Original inputs `_Lots`, `_SL`, `_TP`, `_MagicNumber`, `_Comment`, `_OnlyOneOpenedPos`, and `_AutoDigits` are mapped to StockSharp parameters or built-in behavior.
* Order closure helper routines from the MQL version are replaced with StockSharp high-level protective order management.
* Error handling and retry loops from the MQL code are omitted because the StockSharp execution model already manages order states and retries.
