# Larry Connors RSI-2 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

A faithful StockSharp port of the classic Larry Connors RSI-2 system. The strategy combines a fast 2-period RSI oscillator with moving-average filters on the hourly timeframe to capture short-term mean-reversion setups while staying aligned with the higher timeframe trend. Optional stop-loss and take-profit levels, expressed in pips, replicate the original MetaTrader money-management rules.

## Concept Overview

- **Type**: Mean reversion with trend filter.
- **Market**: Designed for Forex pairs traded on the H1 chart.
- **Direction**: Trades both long and short, but only in the direction of the slow SMA filter.
- **Core Indicators**: 5-period SMA (exit timing), 200-period SMA (trend filter), 2-period RSI (signal trigger).

## Trading Rules

### Long Entries
- RSI value drops below `RSI Long Entry` (default 6).
- The closing price of the completed candle stays above the `Slow SMA` (default 200 periods).
- No open position is present.

### Short Entries
- RSI value rises above `RSI Short Entry` (default 95).
- The closing price is below the `Slow SMA`.
- No open position is present.

### Exit Conditions
- **Long positions** close when the close moves above the `Fast SMA` (default 5). Optional stop-loss and take-profit levels measured in pips can also close the trade if enabled.
- **Short positions** close when the close moves below the `Fast SMA`. Optional stop-loss and take-profit levels measured in pips apply symmetrically.

### Risk Management
- `Use Stop Loss` toggles a fixed stop distance in pips relative to the entry price.
- `Use Take Profit` enables a symmetric profit target in pips.
- Pip distances are converted to absolute prices via the instrument's `PriceStep` and decimal precision, mirroring the MT5 logic for 4/5-digit quotes.

## Default Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `Trade Volume` | 1 | Base order volume for every entry. |
| `Fast SMA Period` | 5 | Exit timing average. |
| `Slow SMA Period` | 200 | Trend direction filter. |
| `RSI Period` | 2 | Lookback for the RSI oscillator. |
| `RSI Long Entry` | 6 | Oversold threshold for long trades. |
| `RSI Short Entry` | 95 | Overbought threshold for short trades. |
| `Use Stop Loss` | true | Enable/disable protective stop. |
| `Stop Loss (pips)` | 30 | Stop-loss distance in pips. |
| `Use Take Profit` | true | Enable/disable fixed profit target. |
| `Take Profit (pips)` | 60 | Profit target distance in pips. |
| `Candle Type` | 1 hour | Timeframe of the working candles. |

All tunable parameters expose `.SetCanOptimize(true)` allowing batch optimization inside Designer/Tester.

## Execution Notes

- Signals are evaluated on closed candles to match the original MetaTrader implementation.
- Protective levels are tracked internally, closing the entire position with market orders when breached.
- The strategy resets internal state (`pipSize`, entry anchors) on each restart to guarantee reproducible backtests.
- Add the strategy to a project alongside reliable Forex data to replicate the published performance results.

## Suggested Use

1. Connect a Forex data feed that supplies 1-hour candles.
2. Add the strategy to Designer or run it programmatically through StockSharp API.
3. Adjust pip-based risk parameters to match the broker's contract specifications if necessary.
4. Optionally optimize RSI thresholds or moving-average lengths to adapt the model to other symbols.

By preserving the exact RSI and moving-average logic, this port allows MT5 users to evaluate the Larry Connors RSI-2 methodology within the StockSharp ecosystem.
