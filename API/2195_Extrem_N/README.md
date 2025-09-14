# Extrem N Strategy

The Extrem N strategy trades reversals based on new highs and lows detected over a rolling window.

The strategy relies on the Donchian Channel indicator to mark price extremes. When a bar sets a new high relative to the lookback period and the following bar sets a new low, a long position is opened. A short position is opened when a new low is followed by a new high. Opposite signals close existing positions.

- **Entry conditions**:
  - Long: previous bar made a new high and current bar made a new low.
  - Short: previous bar made a new low and current bar made a new high.
- **Exit conditions**:
  - Long positions are closed on a short entry signal.
  - Short positions are closed on a long entry signal.
- **Parameters**:
  - `Period` – Donchian lookback period (default 9).
  - `CandleType` – timeframe of processing (default 4 hours).
  - `BuyPosOpen` – allow opening long positions (default true).
  - `SellPosOpen` – allow opening short positions (default true).
  - `BuyPosClose` – allow closing long positions (default true).
  - `SellPosClose` – allow closing short positions (default true).
- **Indicators**: Donchian Channel.
