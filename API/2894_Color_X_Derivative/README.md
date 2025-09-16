# Color X Derivative Strategy

## Overview
This strategy is a StockSharp port of the MetaTrader expert "Exp_ColorXDerivative". It works on a configurable candle timeframe (12-hour candles by default) and analyses the ColorXDerivative momentum histogram. The indicator measures how fast the chosen price source changes over a fixed shift, smooths the result with a moving average, and then classifies each bar into one of five colour states. Trades follow the same logic as in the original EA: the robot buys when bullish momentum accelerates or a bearish move starts to contract, and it sells when bearish pressure increases or a bullish leg loses strength.

## Indicator logic
1. Convert each candle to the selected `AppliedPrice` (close, open, weighted close, Demark, etc.).
2. Compute the price derivative: `(price[0] - price[shift]) * 100 / shift`, where `shift = DerivativePeriod`.
3. Smooth the derivative with the selected method (`SMA`, `EMA`, `SMMA`, `LWMA` or `Jurik`). The default Jurik moving average reproduces the JJMA smoothing from the MQL implementation.
4. Assign a colour state:
   - **0** – derivative &gt; 0 and rising (strong bullish acceleration).
   - **1** – derivative &gt; 0 but falling (bullish momentum losing strength).
   - **2** – derivative ≈ 0 (neutral).
   - **3** – derivative &lt; 0 but rising (bearish move contracting).
   - **4** – derivative &lt; 0 and falling (bearish acceleration).

A signal shift controls which finished bar is evaluated (1 = last closed bar, 2 = previous bar, etc.).

## Trading rules
- **Long entry**: enabled when `EnableLongEntry` is true and either
  - the current colour is 0 while the previous colour was not 0 (momentum turns sharply bullish), or
  - the current colour is 3 while the previous colour was 4 or 2 (bearish move starts to contract).
- **Short entry**: enabled when `EnableShortEntry` is true and either
  - the current colour is 4 while the previous colour was not 4 (bearish acceleration starts), or
  - the current colour is 1 while the previous colour was 0 or 2 (bullish move fades).
- **Long exit**: triggered when the current colour is 1 or 4 and `EnableLongExit` is true.
- **Short exit**: triggered when the current colour is 0 or 3 and `EnableShortExit` is true.

Orders are sent as market orders using the `OrderVolume` parameter. Position closes are executed before new entries to mimic the sequential logic of the original EA.

## Risk management
Optional stop loss and take profit distances are provided via `StopLossTicks` and `TakeProfitTicks`. When either value is above zero the strategy calls `StartProtection`, converting ticks into price steps using the security's `Step` size. The stop/target protection runs once and is compatible with auto-trading or backtesting.

## Parameters
- `OrderVolume` – market order size.
- `CandleType` – timeframe for the indicator calculations (default 12-hour time frame).
- `DerivativePeriod` – distance in bars used for the derivative shift.
- `AppliedPrice` – price source passed into the derivative (close, median, weighted, Demark, etc.).
- `SmoothingMethod` – smoothing filter applied to the derivative. Supported values: SMA, EMA, SMMA, LWMA, Jurik.
- `SmoothingLength` – period of the smoothing filter.
- `SignalShift` – how many finished bars back to read the colour values (1 = most recent closed bar).
- `StopLossTicks` / `TakeProfitTicks` – optional protective distances in security steps.
- `EnableLongEntry`, `EnableShortEntry`, `EnableLongExit`, `EnableShortExit` – toggles matching the original EA inputs.

## Notes
- The strategy reproduces the indicator-driven logic of the MetaTrader EA without additional money-management features.
- Jurik smoothing is the closest approximation to the JJMA filter used in the MQL library; other options map to the standard StockSharp moving averages.
- The colour history is stored internally so optimisation on `SignalShift` works exactly as in the MetaTrader version.
