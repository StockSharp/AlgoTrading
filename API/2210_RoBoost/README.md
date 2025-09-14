# RoBoost Strategy

This strategy is a C# adaptation of the original MQL4 expert advisor **RoBoostj**.
It trades on a single security using RSI based signals combined with simple
price momentum detection. The strategy operates on a selected candle type
(default is 1-hour candles).

## Logic

- When the previous close price is higher than the current close and the RSI
  value falls below the **RSI Down** threshold, the strategy enters a short
  position.
- When the previous close price is lower or equal to the current close and the
  RSI value rises above the **RSI Up** threshold, the strategy enters a long
  position.
- Active positions are managed using the following risk tools:
  - Fixed **Take Profit** and **Stop Loss** levels measured in price units.
  - Optional trailing stop activated when the trade moves in profit by the
    **Trail Start** distance. After activation the stop price follows price by
    the **Trail Step** distance.

## Parameters

| Name            | Description                                                   |
|-----------------|---------------------------------------------------------------|
| `CandleType`    | Candle series used for calculations.                          |
| `RsiPeriod`     | Period length of the RSI indicator.                            |
| `RsiUp`         | RSI threshold used for long entries.                           |
| `RsiDown`       | RSI threshold used for short entries.                          |
| `TakeProfit`    | Take profit distance from entry price (points).                |
| `StopLoss`      | Stop loss distance from entry price (points).                  |
| `UseTrailing`   | Enables trailing stop logic.                                   |
| `TrailStart`    | Distance in points when trailing stop becomes active.          |
| `TrailStep`     | Distance in points maintained from the current price when
                   trailing stop is active.                                       |

All distances are expressed in absolute price units and may require adjustment
according to the instrument tick size.

## Usage

1. Add the strategy to your project or open it inside StockSharp Designer.
2. Configure parameters according to your trading preferences.
3. Start the strategy. It will automatically subscribe to the chosen candle
   series and manage trades based on RSI values and candle closes.

The strategy is intended for educational purposes and should be tested on
historical data before using on live markets.
