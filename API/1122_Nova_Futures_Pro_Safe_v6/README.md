# Nova Futures PRO SAFE v6 Strategy

This strategy combines trend, volatility and structure signals. It uses a 200 EMA with ADX to confirm trends, Bollinger Bands versus Keltner Channels to detect squeeze breakouts, and Donchian levels for structure break of highs or lows. Optional higher timeframe filters and a choppiness index avoid trading in low-quality regimes. A cooldown prevents immediate re-entry after a position closes.

## Inputs
- **EMA Length** — base exponential moving average length
- **DMI Length** — period for ADX and directional movement
- **Min ADX** — minimum ADX value to consider trend
- **BB Length** — Bollinger Bands period
- **BB Mult** — Bollinger Bands multiplier
- **KC Length** — Keltner Channels period
- **KC Mult** — Keltner Channels multiplier
- **Donchian Length** — lookback for structure levels
- **Use HTF** — enable higher timeframe confirmation
- **HTF Candle** — higher timeframe for filters
- **HTF EMA** — EMA length on higher timeframe
- **HTF Min ADX** — minimum ADX on higher timeframe
- **Use Choppiness** — enable choppiness filter
- **Chop Length** — choppiness index period
- **Chop Threshold** — maximum choppiness allowed
- **Cooldown** — bars to wait after an exit
- **Candle Type** — main candle timeframe

## Notes
Simplified port of the TradingView script "Nova Futures PRO (SAFE v6) — HTF + Choppiness + Cooldown".
