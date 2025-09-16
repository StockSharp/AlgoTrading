# CoensioTrader1 V06 Strategy

## Overview
CoensioTrader1 V06 is a trend-following breakout strategy originally distributed as a MetaTrader Expert Advisor. The StockSharp port keeps the discretionary pattern recognition logic while removing the broker- and internet-specific features from the MQL implementation. The strategy operates on a single security and timeframe, using Bollinger Bands and a double exponential moving average (DEMA) to identify exhaustion moves followed by trend resumption.

The original robot allowed trading up to six currency pairs with individual parameter sets, supported DLL-based licensing, and reported optimization results to a remote server. Those auxiliary services are intentionally omitted in this port. The focus is the core entry and exit workflow that reacts to Bollinger Band rejections confirmed by swing structure and the DEMA slope.

## Strategy Logic
1. **Data subscription** – the strategy subscribes to the configured candle type (default: 1 hour) and binds Bollinger Bands together with a DEMA.
2. **Bollinger Band rejection** – signals are evaluated on the last fully closed candle.
   - **Long setup**
     - Candle opened below the previous lower Bollinger Band and closed back above it (failed breakdown).
     - Candle created a higher low compared to the bar before it, while that earlier bar made a lower low compared to its predecessor (double-bottom style structure).
     - The DEMA is strictly rising across the last three observations (current value > previous > second previous).
   - **Short setup**
     - Candle opened above the previous upper Bollinger Band and closed back below it (failed breakout).
     - Candle made a lower high compared to the bar before it, while that earlier bar made a higher high compared to its predecessor (double-top structure).
     - The DEMA is strictly falling across the last three observations.
3. **Order execution** – market orders are sent immediately after the signal is confirmed on a finished candle. Optional position flattening on opposite signals can be enabled.
4. **Risk management** – optional stop-loss and take-profit distances are provided through `StartProtection`. Both are absolute price offsets; trailing stop functionality from the original expert is not reproduced.

## Parameters
| Name | Description | Default |
| ---- | ----------- | ------- |
| `BollingerPeriod` | Period for Bollinger Band calculation. | 30 |
| `BollingerDeviation` | Standard deviation multiplier for the bands. | 1.5 |
| `DemaPeriod` | Length of the double exponential moving average used for trend confirmation. | 20 |
| `StopLossDistance` | Absolute stop-loss offset passed to `StartProtection`. Set to zero to disable. | `0 (absolute)` |
| `TakeProfitDistance` | Absolute take-profit offset passed to `StartProtection`. Set to zero to disable. | `0 (absolute)` |
| `CloseOnSignal` | Close the current position before opening a new one in the opposite direction. | `false` |
| `CandleType` | Candle data type or timeframe. Defaults to 1-hour time frame. | `1h` |

## Usage Notes
- The StockSharp version trades only the primary `Strategy.Security`. To mimic the multi-symbol behavior of the original expert, launch separate strategy instances with distinct parameter sets.
- The MQL lot-sizing logic (`RiskMax`, `LotSize`, `LotBalanceDivider`) was not translated. Configure `Volume` on the strategy or via risk manager according to your portfolio rules.
- The DLL-based activation, remote optimization logging, and UI drawing routines present in the MQL files were intentionally removed.
- Stop-loss and take-profit values are absolute price distances. Adapt them to the instrument’s tick size or pip value when configuring the strategy.
- The original trailing-stop step mechanic is not implemented. If trailing management is required, layer a dedicated risk module on top of this strategy.
- All code comments and logics are kept in English as requested, while README translations are provided separately.

## Differences from the MQL Version
- **Multi-symbol management**: replaced with a single-instrument design for clarity and easier testing.
- **Networking and licensing**: removed; no external HTTP requests or DLL calls are performed.
- **Order sizing**: simplified to rely on StockSharp’s standard `Volume` handling.
- **Visual objects**: chart annotations from MetaTrader (arrows, labels, color themes) are not recreated. Use StockSharp chart helpers if visualization is required.
- **Trailing stop**: not ported; only the initial protective orders are configured.

This documentation aims to be exhaustive so that the port can be reviewed, tested, and extended without needing to read the original MQL source.
