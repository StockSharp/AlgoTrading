# Exp X2MA Candle MM Recovery Strategy

## Overview
This strategy is a C# conversion of the MetaTrader expert **Exp_X2MACandle_MMRec**. It observes the colour of a doubly smoothed candle, produced by the original X2MA custom indicator, to decide when to open or close positions. The StockSharp version recreates the dual smoothing pipeline and keeps a lightweight money management layer that cuts the trading volume after a configurable number of recent losses.

The algorithm processes completed candles only. It subscribes to a configurable timeframe, applies two chained moving averages to the candle OHLC values, derives a synthetic candle colour (green, grey or red) and uses colour transitions with a user-selectable bar shift to trigger actions. Long trades are opened when the colour changes from bullish to anything else. Short trades follow the symmetric condition. Position exits are aligned with the same colour checks and can be enabled or disabled separately for each side.

## Indicator logic
1. Each candle is smoothed twice. Both stages may use different methods and lengths.
2. Smoothing options map to StockSharp indicators:
   - `Simple` → `SimpleMovingAverage`
   - `Exponential` → `ExponentialMovingAverage`
   - `Smoothed` → `SmoothedMovingAverage` (RMA)
   - `Weighted` → `WeightedMovingAverage`
   - `Jurik` → `JurikMovingAverage` (Phase parameter is honoured when available).
3. The synthetic candle body is flattened whenever the absolute open/close difference is below `GapPoints * Security.StepPrice`.
4. Colours are assigned as follows: open < close → `2` (bullish), open > close → `0` (bearish), otherwise → `1` (neutral).
5. Signals are evaluated on bar `SignalBar + 1` (two bars back with the default setting) so that orders are submitted only after a full candle confirms the colour change.

## Money management
- The original expert dynamically reduced the position size after a series of losses using historical deal statistics. StockSharp does not expose the exact MetaTrader history, so the port keeps an internal queue of recent closed trades.
- The queue length is controlled by `HistoryDepth` and the volume falls back to `ReducedVolume` once `LossTrigger` or more losses are detected inside the window.
- The strategy records trade outcomes using candle close prices when a manual exit is triggered. Stop-loss/take-profit orders from the MetaTrader version are not recreated. You can add your own protection rules through StockSharp's risk managers if required.

## Parameters
| Name | Description |
|------|-------------|
| `CandleType` | Timeframe of the candles used for smoothing and trading. |
| `FirstMethod`, `FirstLength`, `FirstPhase` | Primary smoothing method, length and Jurik phase. |
| `SecondMethod`, `SecondLength`, `SecondPhase` | Secondary smoothing method, length and Jurik phase. |
| `GapPoints` | Body flattening threshold in price steps. |
| `SignalBar` | Shift (0 = latest finished candle) used when reading the colour buffers. |
| `AllowLongEntry` / `AllowShortEntry` | Enable opening long or short positions. |
| `AllowLongExit` / `AllowShortExit` | Enable closing long or short positions. |
| `NormalVolume` | Standard order size (lots, shares, contracts). |
| `ReducedVolume` | Order size used after the configured number of losses. |
| `HistoryDepth` | Number of recent trades inspected for losses (0 disables history tracking). |
| `LossTrigger` | Loss count that activates the reduced volume (0 disables the switch). |

## Usage notes
- The strategy operates on a single security returned by `GetWorkingSecurities()`.
- Signals and exits are processed once per finished candle to avoid duplicate orders.
- Set `ReducedVolume` equal to `NormalVolume` if you want to disable the volume reduction while keeping the history statistics.
- Because the port relies on candle close prices to classify trades, the loss counter may differ slightly from MetaTrader when slippage or partial fills occur. The documentation should help you adjust parameters to achieve similar behaviour.
- Stops and take profits from the MQL version are not recreated automatically. Use StockSharp risk managers (`StartProtection`) if you need platform-level protection.
