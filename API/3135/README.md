# Advanced EA Panel Strategy

This strategy is a StockSharp port of the **Advanced EA Panel** utility from MQL5. The original expert advisor provided a manual trading dashboard with multi-timeframe analytics, pivot management, and quick trade buttons. The C# implementation re-creates those analytical capabilities inside an automated strategy so they remain available without an on-chart control panel.

## Key features

- Aggregates nine timeframes (M1 … MN1) and tracks EMA(3/6/9), SMA(50/200), CCI(14) and RSI(21) votes for each horizon.
- Calculates floor-trader, Woodie or Camarilla pivot levels on a configurable candle series.
- Monitors volatility with an ATR feed and logs every significant change.
- Keeps an internal risk panel by computing stop distance, reward distance and live risk/reward ratio for the active position.
- Supports automatic order execution when the multi-timeframe vote exceeds a configurable threshold. Opposite trades are flattened before reversing, exactly like pressing the panel buttons.
- Leverages `StartProtection` so stop-loss and take-profit guards survive restarts, mirroring the protection logic of the original panel.

## Trading logic

1. Each timeframe subscription produces indicator values for EMA(3/6/9), SMA(50/200), CCI(14) and RSI(21). A bullish vote is added when the close is above the moving averages, CCI is above +100, and RSI is above 60. Bearish votes are produced for the opposite conditions. Neutral readings do not contribute to the score.
2. The total score across ready timeframes is compared against `DirectionalThreshold`. Scores ≥ threshold generate a **Buy** signal; scores ≤ –threshold generate a **Sell** signal.
3. When auto trading is enabled the strategy:
   - Closes the opposite position with `ClosePosition()` before sending the reversing order.
   - Sends a market order sized according to `Volume`, rounded to the nearest `Security.VolumeStep`.
   - Relies on `StartProtection` to attach stop-loss/take-profit brackets expressed in pips.
4. ATR from the primary candle series is logged. Any change beyond the rounding precision prints a fresh volatility report.
5. Pivot levels are recomputed whenever the pivot timeframe produces a finished candle. The log shows PP, R1–R4 and S1–S4 so they can be used as discretionary levels or exported to dashboards.

## Parameters

| Name | Description | Group | Default |
| --- | --- | --- | --- |
| `Volume` | Trading volume in lots. Rounded to `VolumeStep` before sending orders. | Trading | 1.0 |
| `StopLossPips` | Distance from entry to stop-loss expressed in price steps. `0` disables the stop. | Risk | 50 |
| `TakeProfitPips` | Distance from entry to take-profit in price steps. `0` disables the take. | Risk | 100 |
| `VolatilityPeriod` | ATR lookback length used for volatility logging. | Volatility | 14 |
| `PrimaryCandleType` | Candle type driving ATR calculations and chart drawing. | General | 15 minute candles |
| `PivotCandleType` | Candle type used for pivot level recalculation. | General | 1 hour candles |
| `DirectionalThreshold` | Absolute score required to trigger a Buy/Sell signal. | Signals | 3 |
| `AutoTradingEnabled` | Enables automatic execution of detected signals. | Signals | true |
| `PivotFormula` | Pivot preset (`Classic`, `Woodie`, `Camarilla`). | General | Classic |

## Risk management

- `StartProtection` attaches price-based brackets calculated from `StopLossPips` and `TakeProfitPips` (converted to absolute price using `PriceStep`).
- `_entryPrice`, `_stopPrice` and `_takePrice` are updated on fills so the strategy can log risk, reward and risk/reward ratio in pips.
- If auto trading is disabled the risk monitor still works for manual entries executed outside the strategy.

## Differences from the MQL5 panel

- The original EA displayed buttons and draggable lines on the chart; the StockSharp version exposes the same analytics through logs and strategy parameters. All comments inside the code explain how to extend or hook the results into a UI if required.
- Position management is automated. Clicking **Buy**, **Sell**, **Reverse** or **Close** is replaced by `RequestExecution`, `SendOrder` and `ClosePosition()` in reaction to the multi-timeframe score.
- Points of interest, manual tab edits and chart object manipulation are not ported. Instead, pivots are recalculated programmatically and logged. Traders can consume the log or extend the strategy to draw objects if desired.
- Volatility, risk metrics and pivots persist across restarts because they are recalculated from live data instead of relying on chart objects.

## Usage notes

1. Attach the strategy to a symbol and ensure the connector provides all candle types listed in `PanelTimeFrames`. Missing data will delay signal generation until at least one candle per timeframe is finished.
2. Adjust `DirectionalThreshold` to control sensitivity. Higher thresholds demand more agreement across timeframes before trading.
3. Set `AutoTradingEnabled = false` to use the module as an informational dashboard while placing orders manually from another tool.
4. The class adds default chart rendering for primary candles, ATR and own trades. Remove or extend these calls if a custom visualization is required.

## Conversion summary

- **UI actions → Strategy methods.** Panel button handlers (`EAPanelClickHandler`, `T0ClickHandler`, etc.) are mapped to order execution helpers that preserve the buy/sell/reverse/close flow.
- **Pivot formulas.** The MQL5 spinners allowed independent formulas per level; this port keeps the preset combinations (`Classic`, `Woodie`, `Camarilla`) that the panel offered via its quick-select buttons.
- **Indicator tracking.** Native MQL5 indicator handles are replaced by `ExponentialMovingAverage`, `SimpleMovingAverage`, `CommodityChannelIndex` and `RelativeStrengthIndex` from StockSharp with `Bind` callbacks.
- **Risk panel.** All risk/rward calculations that were previously rendered in edit boxes are now logged and can be consumed by any monitoring component.

The strategy therefore preserves the intent of the Advanced EA Panel—centralized situational awareness with quick reaction logic—while presenting it as a fully automated StockSharp strategy ready for optimization or discretionary monitoring.
