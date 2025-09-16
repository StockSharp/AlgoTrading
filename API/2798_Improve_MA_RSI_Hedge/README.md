# Improve MA & RSI Hedge Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy ports the original MetaTrader "Improve" expert to StockSharp using the high-level API. It simultaneously trades two instruments: the main symbol selected for the strategy and a hedge symbol. Trade direction is defined by the relationship between two smoothed moving averages on the main instrument and the relative strength index (RSI). The hedge leg mirrors the direction of the main leg, creating a paired exposure that seeks to profit from synchronized momentum moves while limiting single-instrument risk.

## Strategy Logic

- Compute two Smoothed Moving Averages (SMMA) on the primary symbol with configurable fast and slow periods.
- Calculate RSI on the same candles and monitor oversold/overbought thresholds.
- Enter **long** on both instruments when the slow SMMA is above the fast SMMA and RSI is at or below the oversold threshold.
- Enter **short** on both instruments when the slow SMMA is below the fast SMMA and RSI is at or above the overbought threshold.
- Positions stay open until the combined open profit of both legs exceeds the configured money target, at which point the strategy liquidates both sides.

The algorithm keeps track of the most recent closing prices of each instrument. Combined profit is estimated from the difference between the current close and the stored entry price of each leg. Because no stop-loss is applied, positions can remain open for extended periods when price fails to reach the profit target.

## Parameters

| Parameter | Description |
| --- | --- |
| **Volume** | Order quantity for both the primary and hedge instruments. |
| **Profit Target** | Monetary target shared by both legs; when reached the strategy closes every open position. |
| **Hedge Security** | Secondary instrument that is traded alongside the primary security. |
| **Fast MA** | Period of the fast Smoothed Moving Average (default 8). |
| **Slow MA** | Period of the slow Smoothed Moving Average (default 21). Must be greater than the fast MA period. |
| **RSI Period** | Length used to compute RSI (default 21). |
| **Oversold** | RSI level that triggers long entries together with the MA condition (default 30). |
| **Overbought** | RSI level that triggers short entries together with the MA condition (default 70). |
| **Candle Type** | Time frame for calculations; defaults to 1-hour candles but can be adjusted. |

## Indicators

- **Smoothed Moving Average (SMMA)** – used twice to define the fast and slow trend components.
- **Relative Strength Index (RSI)** – determines oversold/overbought conditions for confirmation.

## Entry and Exit Rules

1. **Long Entry**
   - Slow SMMA &gt; Fast SMMA on the primary symbol.
   - RSI ≤ Oversold.
   - Both legs are opened with market orders in the same direction (buy/buy).
2. **Short Entry**
   - Slow SMMA &lt; Fast SMMA on the primary symbol.
   - RSI ≥ Overbought.
   - Both legs are opened with market orders in the same direction (sell/sell).
3. **Exit**
   - When `(primary profit + hedge profit) ≥ Profit Target`, the strategy closes both positions using market orders.
   - No additional stop-loss or trailing logic is applied; risk management should be added externally if required.

## Usage Notes

- Ensure that both the primary security and the hedge security are assigned before starting the strategy; otherwise it will throw an exception.
- The combined profit estimate relies on candle close prices. Slippage and execution differences between the two legs can affect actual realized profit.
- Because the strategy opens both legs simultaneously, it is suited for correlated instruments (for example, currency pairs or related futures) where moving in tandem is expected.
- Consider adding portfolio-level risk controls when trading live, as the original algorithm uses only the virtual profit target for exits.
