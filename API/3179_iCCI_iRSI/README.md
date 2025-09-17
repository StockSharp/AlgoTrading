# iCCI iRSI Strategy

## Overview
The **iCCI iRSI Strategy** is a direct conversion of the MetaTrader 5 expert advisor `iCCI iRSI.mq5`. The original system blends the Commodity Channel Index (CCI) and the Relative Strength Index (RSI) to detect exhaustion zones. When both oscillators agree on an oversold or overbought state the advisor opens a position, attaches protective orders and optionally trails the stop as the trade moves into profit. This StockSharp port mirrors that behaviour with high-level APIs, including pip-based inputs, auto-closing of opposite positions and a reversible signal mode.

## Trading Logic
1. Subscribe to the configured candle type and calculate a `CommodityChannelIndex` with period `CciPeriod` and a `RelativeStrengthIndex` with period `RsiPeriod`.
2. Evaluate only completed candles. Intrabar noise is ignored just like the MQL implementation that waits for a new bar.
3. When both indicators fall below their respective lower thresholds (`CciLowerLevel` and `RsiLowerLevel`) the strategy opens or reverses into a long position. When both indicators rise above the upper thresholds (`CciUpperLevel` and `RsiUpperLevel`) a short setup is triggered. Enabling `ReverseSignals` swaps the directions.
4. Before submitting a new order the current opposite exposure is closed so the net position always matches the active signal.
5. After entry the strategy monitors the close price of subsequent candles. Take-profit and stop-loss levels expressed in pips are converted to price units using the instrument’s `PriceStep`. For 3- or 5-digit forex symbols an additional ×10 adjustment reproduces the MetaTrader pip definition.
6. If `TrailingStopPips` is positive, the stop-loss is advanced toward the market whenever price moves more than `TrailingStopPips + TrailingStepPips` in the favourable direction. Updates respect the configured step to avoid rapid stop modifications.

## Risk and Trade Management
- **Take-profit / Stop-loss** – optional pip distances that become absolute price levels immediately after a fill. When either level is breached on the close of a candle the position is liquidated at market.
- **Trailing stop** – mimics the EA’s trailing logic. Profits must exceed the trailing distance plus the trailing step before the stop is tightened.
- **Volume** – a fixed `TradeVolume` parameter replaces the original lot-or-risk switch (`ENUM_LOT_OR_RISK`). Use optimisation to discover suitable volumes if money-management variants are required.
- **Position hygiene** – when a new signal appears the strategy flattens any opposite holdings before opening the fresh trade, just as the EA performs `ClosePositions`.

## Parameters
- **Candle Type** – candle data series processed by the indicators (default: 1-hour candles).
- **CciPeriod** – CCI averaging length (default: 14).
- **CciUpperLevel / CciLowerLevel** – overbought and oversold CCI thresholds (defaults: +80 / −80).
- **RsiPeriod** – RSI averaging length (default: 42).
- **RsiUpperLevel / RsiLowerLevel** – RSI trigger levels (defaults: 60 / 30).
- **ReverseSignals** – flips the interpretation of the oscillator signals (default: `false`).
- **TradeVolume** – market order size. Set to match the MT5 lot input (default: 0.1).
- **StopLossPips / TakeProfitPips** – protective distances in pips (defaults: 0 and 140). Set to zero to disable.
- **TrailingStopPips / TrailingStepPips** – trailing-stop distance and minimum step (defaults: 5 / 5). A zero trailing distance disables trailing even if a step is provided.

## Implementation Notes
- StockSharp indicators (`CommodityChannelIndex`, `RelativeStrengthIndex`) deliver ready-to-use decimal values through the `Bind` API, so no manual `CopyBuffer` logic is required.
- All trade management takes place on finished candles. This matches the EA’s `PrevBars` guard and prevents multiple entries within the same bar.
- Pip conversion honours fractional pip quotes by multiplying the `PriceStep` by 10 for instruments with 3 or 5 decimals – a direct analogue to the MQL `digits_adjust` logic.
- Protective targets are simulated via market exits because StockSharp strategies operate inside a sandboxed environment where synchronous order modifications are not available.
- Additional chart areas draw the CCI and RSI lines for visual validation of entry zones.

## Differences from the Original Expert Advisor
- The MetaTrader module `MoneyFixedMargin` is not ported. Position sizing is now a simple fixed volume parameter.
- Broker-specific checks such as `FreezeStopsLevels` are not available in StockSharp. The trailing stop therefore only observes price distance and step requirements.
- Logging and alert strings have been removed in favour of clean strategy output. StockSharp’s logging system can be attached externally if needed.
- Trade management operates on candle closes. The MT5 version could react intrabar when the stop or take-profit is touched, but the end-of-bar approximation keeps the logic deterministic for backtests.

## Usage Tips
1. Start with the default 1-hour timeframe to mirror the original template. Shorter frames can introduce more signals but also more whipsaws.
2. Optimise `CciUpperLevel`, `CciLowerLevel`, `RsiUpperLevel` and `RsiLowerLevel` together – the EA relies on agreement between both oscillators, so balanced thresholds are essential.
3. When running on forex pairs double-check that the security metadata exposes `PriceStep` and `Decimals` so pip distances convert correctly.
4. Disable `ReverseSignals` for classical trend-reversal behaviour. Enable it to trade breakouts out of overbought/oversold zones.
5. Combine with StockSharp risk modules (equity stop, drawdown protection) if portfolio-level controls are required – they replace the MT5 `m_money` helper.

This documentation should provide all necessary context to deploy, customise and extend the iCCI iRSI strategy within the StockSharp environment.
