# ColorMetroDuplexStrategy

## Overview

`ColorMetroDuplexStrategy` is a C# conversion of the MetaTrader 5 expert **Exp_ColorMETRO_Duplex**. The original robot uses two independent instances of the ColorMETRO indicator to manage long and short trading modules. Each module operates on its own candle subscription, evaluates two stepped RSI envelopes produced by the ColorMETRO indicator, and optionally opens or closes positions when the fast and slow envelopes cross.

The StockSharp version keeps both modules and reproduces the same signal evaluation rules while using the high-level API for candle subscriptions, order management and indicator binding. A custom `ColorMetroIndicator` is included to mimic the MT5 iCustom implementation, exposing the fast and slow ColorMETRO bands together with the inner RSI value.

## How it works

1. Two `SignalModule` instances are created — **Long** and **Short** — each with its own candle series, ColorMETRO settings and trade management options.
2. When the strategy starts, every module subscribes to its configured timeframe and binds the `ColorMetroIndicator` through `SubscribeCandles(...).BindEx(...)`.
3. For every finished candle the indicator produces:
   - The fast ColorMETRO band (fast RSI envelope).
   - The slow ColorMETRO band (slow RSI envelope).
   - The underlying RSI value (used for reference only).
4. The module stores the indicator history and evaluates the last two values using the configured `SignalBar` shift (matching the `CopyBuffer` logic from MT5).
5. Trading rules:
   - **Long module**
     - *Open*: the fast band was above the slow band on the previous bar and is now below or equal to it.
     - *Close*: the slow band was above the fast band on the previous bar.
   - **Short module**
     - *Open*: the fast band was below the slow band on the previous bar and is now above or equal to it.
     - *Close*: the slow band was below the fast band on the previous bar.
6. Orders are routed via `BuyMarket` / `SellMarket`. The current net position is respected — opposite trades flatten the existing exposure before opening a new one.

## Parameters

Each module exposes a dedicated parameter group. Defaults mirror the MT5 expert.

### Shared market parameters

- **Long_Volume**, **Short_Volume** — trade size (lots) used for new entries.
- **Long_OpenAllowed**, **Short_OpenAllowed** — enable or disable opening trades for the module.
- **Long_CloseAllowed**, **Short_CloseAllowed** — enable or disable automatic exits.
- **Long_MarginMode**, **Short_MarginMode** — money-management mode kept for compatibility (no effect in this port).
- **Long_StopLoss**, **Long_TakeProfit**, **Long_Deviation**, **Short_StopLoss**, **Short_TakeProfit**, **Short_Deviation** — reserved for documentation; stops and slippage control are not automated in this version.
- **Long_Magic**, **Short_Magic** — original MT5 magic numbers preserved for reference.

### Indicator parameters

- **Long_CandleType**, **Short_CandleType** — timeframe for each ColorMETRO module.
- **Long_PeriodRSI**, **Short_PeriodRSI** — RSI length used inside the ColorMETRO algorithm.
- **Long_StepSizeFast**, **Short_StepSizeFast** — step (in RSI points) for the fast envelope.
- **Long_StepSizeSlow**, **Short_StepSizeSlow** — step for the slow envelope.
- **Long_SignalBar**, **Short_SignalBar** — bar shift used when reading the indicator buffers (identical to the MT5 `SignalBar` input).
- **Long_AppliedPrice**, **Short_AppliedPrice** — price source for RSI calculation (close price by default).

## Differences compared to MT5

- **Position model** — StockSharp strategies work with the net position. The original expert stored separate positions via magic numbers; the port flattens the current exposure before opening the opposite side.
- **Money management** — margin modes and deviation settings are preserved as parameters but not applied automatically. Use the `Volume` inputs to control size.
- **Stop-loss / take-profit** — the MT5 expert placed protective stops with each order. The StockSharp version keeps the distances as parameters for reference, but actual stop orders must be implemented separately if needed.
- **Time level control** — the MT5 code used global variables to ensure only one trade per signal time. In StockSharp we process each finished candle once and rely on the net position check to prevent duplicate entries.

## Notes

- The custom `ColorMetroIndicator` reproduces the MT5 logic, including the stepped RSI envelopes and trend memory. It exposes the fast/slow bands and the internal RSI for charting or debugging.
- Commentary within the code is intentionally verbose to clarify the porting decisions and assist with further customization.
- To enable stop-loss or take-profit automation, extend `SignalModule.ProcessModule` to place protective orders using StockSharp's risk controls.
