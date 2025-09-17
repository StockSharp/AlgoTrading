# Engulfing MFI Confirmation Strategy

This strategy replicates the MetaTrader expert "Expert_ABE_BE_MFI" by combining Japanese candlestick engulfing patterns with confirmation from the Money Flow Index (MFI) oscillator. A long position is opened when a bullish engulfing candle appears while money flow stays in an oversold zone. A short position is opened when a bearish engulfing candle forms under overbought money-flow conditions. Positions are closed when MFI crosses dynamic exit thresholds, signalling momentum reversals.

## Core Idea

1. **Pattern detection** – the body of the current finished candle must fully engulf the previous candle in the direction of the trade.
2. **Volume confirmation** – the MFI indicator (length configurable, default 37) must be below the oversold level (40) for long entries or above the overbought level (60) for short entries.
3. **Momentum exits** – open positions are closed when MFI crosses key reversal levels (30 and 70) in the opposite direction, mimicking the original voting logic of the MQL expert.

## Indicators

- **Money Flow Index (MFI)** – calculates volume-adjusted momentum. The strategy stores the last two MFI readings to detect level crossings.
- **Candlestick Body Analysis** – no additional indicator is registered; engulfing detection uses the latest two completed candles.

## Trading Rules

### Long Entry

- Previous candle is bearish and current candle is bullish.
- Current candle body opens below or equal to the previous close and closes above or equal to the previous open (strict engulfing).
- Latest MFI value is below the configurable `OversoldLevel` (default 40).

### Short Entry

- Previous candle is bullish and current candle is bearish.
- Current candle body opens above or equal to the previous close and closes below or equal to the previous open.
- Latest MFI value is above the configurable `OverboughtLevel` (default 60).

### Exit Conditions

- **Close Short** when MFI crosses above `ExitLongLevel` (30) or `ExitShortLevel` (70) from below.
- **Close Long** when MFI crosses below `ExitShortLevel` (70) or `ExitLongLevel` (30) from above.

These exit thresholds recreate the double voting logic of the original expert, ensuring that extended moves in money flow trigger timely liquidation of positions.

### Trade Management

- Market orders (`BuyMarket` / `SellMarket`) are used for entries and exits.
- No explicit stop-loss or take-profit is used; risk management relies on the MFI reversal signals.

## Parameters

| Name | Description | Default | Range / Notes |
| ---- | ----------- | ------- | ------------- |
| `CandleType` | Candle timeframe used for analysis. | 1 minute | Any supported candle type. |
| `MfiPeriod` | Length of the Money Flow Index. | 37 | Must be > 0; matches original EA default. |
| `OversoldLevel` | MFI level that confirms bullish engulfing setups. | 40 | Enable optimization if needed. |
| `OverboughtLevel` | MFI level that confirms bearish engulfing setups. | 60 | Enable optimization if needed. |
| `ExitLongLevel` | Lower MFI boundary for detecting reversals. | 30 | Used for both long exits and short confirmations. |
| `ExitShortLevel` | Upper MFI boundary for detecting reversals. | 70 | Used for both short exits and long confirmations. |

## Notes on Conversion

- The original MQL expert aggregated “votes” from engulfing patterns and MFI filters. The C# strategy reproduces the same decision flow by directly converting the voting rules into discrete entry and exit conditions.
- Money management and trailing modules from the MQL version are omitted; StockSharp position sizing is controlled by the strategy volume.
- All indicator bindings leverage the high-level API (`SubscribeCandles().Bind(...)`) as required.

## Usage Tips

- Optimise `MfiPeriod`, `OversoldLevel`, and `OverboughtLevel` to adapt the strategy to specific markets.
- Combine with risk controls (protective stops) via `StartProtection` in the host application if additional safety is required.
- Ensure sufficient historical data so that the Money Flow Index is fully formed before enabling trading.
