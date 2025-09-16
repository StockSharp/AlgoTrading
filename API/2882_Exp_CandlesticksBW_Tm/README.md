# Exp CandlesticksBW Tm Strategy

This strategy reproduces the MetaTrader expert **Exp_CandlesticksBW_Tm** on top of the StockSharp high-level API. It relies on Bill Williams' Candlesticks BW indicator, which paints candle colors by combining the Awesome Oscillator (AO) and the Accelerator Oscillator (AC). Momentum acceleration and deceleration are detected via candle color transitions, while an optional trading-session filter restricts entries to specific intraday hours.

## How it works

1. Subscribes to the configured time-frame (default **H4**) and feeds every finished candle into an Awesome Oscillator (5/34). The AO series is smoothed with a 5-period simple moving average to produce the Accelerator component.
2. Each candle is classified into one of six color states: two bullish momentum colors (AO and AC rising), two bearish momentum colors (AO and AC falling), and two neutral colors (mixed AO/AC direction). The candle body direction decides between the darker or lighter shade in each pair.
3. A circular buffer stores the recent color indices alongside their open times. The **SignalBar** parameter selects which historical bar to evaluate (default = previous candle, i.e., offset 1). One bar further back is used as context.
4. Long entries are enabled when the older bar belonged to a bullish momentum zone and the signal bar leaves that zone. Short entries mirror the logic with bearish zones. Exit signals use the same momentum filters to close the opposite direction.
5. Optional session filtering (**UseTimeFilter**) keeps a trade log between **StartHour:StartMinute** and **EndHour:EndMinute**. Leaving the window immediately liquidates open positions, preventing overnight exposure.
6. Stop-loss and take-profit protections are registered through `StartProtection`, translating point-based distances into instrument price steps.

## Trading rules

- **Open long**: previous color index `< 2` (AO and AC accelerating upwards) and the signal bar color index `> 1`. Long entries are skipped if already long or if longs are disabled.
- **Open short**: previous color index `> 3` (AO and AC accelerating downwards) and the signal bar color index `< 4`.
- **Close long**: triggered when the older color index `> 3` (bearish acceleration) and long exits are enabled.
- **Close short**: triggered when the older color index `< 2` (bullish acceleration) and short exits are enabled.
- When the time filter is active, positions are force-closed outside of the allowed session even without color-based exit signals.

## Parameters

| Name | Description | Default |
| --- | --- | --- |
| `CandleType` | Time-frame used for AO/AC calculations. | `TimeSpan.FromHours(4).TimeFrame()` |
| `Volume` | Order size for new entries. | `1m` |
| `SignalBar` | Number of finished candles to skip before evaluating signals (1 = previous candle). | `1` |
| `StopLossPoints` | Protective stop distance in instrument points. Set `0` to disable. | `1000m` |
| `TakeProfitPoints` | Profit target distance in instrument points. Set `0` to disable. | `2000m` |
| `EnableLongEntries`, `EnableShortEntries` | Allow opening trades in the respective direction. | `true` |
| `EnableLongExits`, `EnableShortExits` | Allow closing trades in the respective direction. | `true` |
| `UseTimeFilter` | Enable trading-session restrictions. | `true` |
| `StartHour`, `StartMinute`, `EndHour`, `EndMinute` | Session boundaries (inclusive start, exclusive end for identical hours). | `0/0/23/59` |

## Notes

- The strategy automatically synchronizes the stop-loss and take-profit distances with the instrument price step.
- Signals are timestamped using the close time of the evaluated bar so that repeated trades within the same bar are suppressed.
- No Python version is provided, matching the source MQL package structure.
