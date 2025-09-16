# Exp Skyscraper Fix Color AML MMRec Strategy

Exp Skyscraper Fix Color AML MMRec is the StockSharp port of the MQL5 expert advisor *Exp_Skyscraper_Fix_ColorAML_MMRec*. The original robot combines two independent indicators — **Skyscraper Fix** and **Color AML** — and applies the MMRec money management logic to reduce order size after consecutive losses. The C# implementation keeps both signal sources and the adaptive position sizing while using StockSharp's high-level API for order routing.

## Trading workflow

1. **Skyscraper Fix module** builds an adaptive channel from the finished candles of `SkyscraperCandleType`. When the channel color turns teal (trend &gt; 0) every short position can be closed and, if the previous color was not teal, a new long trade is opened. When the color becomes red (trend &lt; 0) the logic mirrors for short trades. The helper class `SkyscraperFixIndicator` is reused from strategy `3040_Exp_Skyscraper_Fix_Duplex`.
2. **Color AML module** processes candles from `ColorAmlCandleType`. The translated `ColorAmlIndicator` reproduces the adaptive market level and emits a color code: `2` (bullish), `0` (bearish) or `1` (neutral). The module closes the opposite side whenever a bullish or bearish color is detected and opens a fresh position if the color changed from the previous delayed sample.
3. **Signal delay** is controlled independently for both modules through `SkyscraperSignalBar` and `ColorAmlSignalBar`. The strategy keeps queues of indicator outputs and executes orders only after the configured number of closed candles, matching the `CopyBuffer(..., shift, ...)` behaviour in the expert advisor.
4. **Risk management** mirrors the original stop/take-profit distances. Each module defines its own protective distances in price steps (ticks). The strategy translates them to absolute prices and, on every finished candle, checks whether the bar's range touched a stop-loss or a take-profit. If so, the position is flattened with a market order and all protective levels are cleared.
5. **MMRec money management** tracks consecutive losing trades separately for Skyscraper long, Skyscraper short, Color AML long and Color AML short entries. When the loss streak for a direction reaches the corresponding trigger (`*LossTrigger`), the volume switches from `*Mm` to the reduced value `*SmallMm`. Once a profitable trade appears, the streak resets to zero. Because the sample strategy runs on a single netting position, only the `Lot` management mode has practical effect; other modes fall back to direct lot sizing.

## Implementation notes

- The code relies exclusively on StockSharp's high-level API: candle subscriptions feed both indicators and all trading decisions are executed through `BuyMarket`, `SellMarket` and `ClosePosition` helpers.
- Protective orders are implemented with market exits rather than separate stop/limit orders. This avoids conflicts when both modules share the same net position.
- Money management uses execution data received in `OnOwnTradeReceived` to determine the result of the previous trade. The module that opened the position stores its identifier so that the correct loss counter is updated when the position is closed.
- The translated `ColorAmlIndicator` caches candles and smoothing values to follow the original exponential smoothing scheme, including the dynamic alpha based on fractal ranges and the color coding logic (blue for rising AML, red for falling, grey otherwise).
- Magic numbers and explicit slippage settings from the MQL5 version are not required in StockSharp and therefore are omitted.

## Parameters

### Skyscraper Fix module

| Parameter | Default | Description |
| --- | --- | --- |
| `SkyscraperCandleType` | H4 candles | Timeframe used to calculate the Skyscraper Fix channel. |
| `SkyscraperLength` | 10 | ATR lookback used to define the adaptive channel step. |
| `SkyscraperKv` | 0.9 | Multiplier applied to the ATR-based step size. |
| `SkyscraperPercentage` | 0 | Percentage offset applied to the midline. |
| `SkyscraperMode` | HighLow | Price source for the envelope (high/low or close). |
| `SkyscraperSignalBar` | 1 | Number of closed candles to delay Skyscraper signals. |
| `SkyscraperEnableLongEntry` | true | Allow long entries when the channel turns bullish. |
| `SkyscraperEnableShortEntry` | true | Allow short entries when the channel turns bearish. |
| `SkyscraperEnableLongExit` | true | Close long positions on bearish Skyscraper signals. |
| `SkyscraperEnableShortExit` | true | Close short positions on bullish Skyscraper signals. |
| `SkyscraperBuyLossTrigger` | 2 | Consecutive long losses required to switch to the reduced volume. |
| `SkyscraperSellLossTrigger` | 2 | Consecutive short losses required to switch to the reduced volume. |
| `SkyscraperSmallMm` | 0.01 | Order volume used after the loss trigger is reached. |
| `SkyscraperMm` | 0.1 | Default order volume for Skyscraper signals. |
| `SkyscraperMmMode` | Lot | Money management mode (only `Lot` affects the C# port). |
| `SkyscraperStopLossTicks` | 1000 | Stop-loss distance in price steps. A value of 0 disables the stop. |
| `SkyscraperTakeProfitTicks` | 2000 | Take-profit distance in price steps. A value of 0 disables the target. |

### Color AML module

| Parameter | Default | Description |
| --- | --- | --- |
| `ColorAmlCandleType` | H4 candles | Timeframe used by the Color AML indicator. |
| `ColorAmlFractal` | 6 | Fractal window for the AML range calculations. |
| `ColorAmlLag` | 7 | Smoothing lag for the AML exponential averaging. |
| `ColorAmlSignalBar` | 1 | Number of closed candles to delay Color AML signals. |
| `ColorAmlEnableLongEntry` | true | Allow long entries when AML turns bullish (color 2). |
| `ColorAmlEnableShortEntry` | true | Allow short entries when AML turns bearish (color 0). |
| `ColorAmlEnableLongExit` | true | Close long positions on bearish AML colors. |
| `ColorAmlEnableShortExit` | true | Close short positions on bullish AML colors. |
| `ColorAmlBuyLossTrigger` | 2 | Consecutive long losses before switching to the reduced volume. |
| `ColorAmlSellLossTrigger` | 2 | Consecutive short losses before switching to the reduced volume. |
| `ColorAmlSmallMm` | 0.01 | Order volume used after the loss trigger is reached. |
| `ColorAmlMm` | 0.1 | Default order volume for Color AML signals. |
| `ColorAmlMmMode` | Lot | Money management mode (only `Lot` affects the C# port). |
| `ColorAmlStopLossTicks` | 1000 | Stop-loss distance in price steps. Set to 0 to disable. |
| `ColorAmlTakeProfitTicks` | 2000 | Take-profit distance in price steps. Set to 0 to disable. |

## Usage

1. Attach the strategy to a portfolio and the instrument you wish to trade. The security must provide the candle series defined by `SkyscraperCandleType` and `ColorAmlCandleType`.
2. Adjust the money-management parameters if your broker uses a different lot step. Only direct lot sizing is applied by the port, so configure `*Mm` and `*SmallMm` accordingly.
3. Optionally modify the stop-loss and take-profit distances (in ticks) for each module. Setting a distance to zero disables the corresponding protection.
4. Start the strategy. It will subscribe to both candle streams, calculate the indicators, and manage entries and exits automatically according to the rules above.

The README reflects the behaviour of `CS/ExpSkyscraperFixColorAmlMmrecStrategy.cs` and should be used as the reference documentation for this StockSharp implementation.
