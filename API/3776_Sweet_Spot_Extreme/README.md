# Sweet Spot Extreme Strategy

Sweet Spot Extreme is a direct port of the MetaTrader 4 expert advisor "Sweet_Spot_Extreme.mq4" built on StockSharp's high-level API. The strategy hunts for strong pullbacks inside an existing trend by combining two exponential moving averages on 15-minute candles with a 30-minute Commodity Channel Index (CCI) filter. Position sizing mirrors the original risk controls, including MetaTrader-style lot reduction after losing streaks.

## Core logic

1. **Trend slope confirmation.** The main EMA (`MaPeriod`, default 85) and the close EMA (`CloseMaPeriod`, default 70) are fed with 15-minute median prices. A long setup requires both EMAs to slope upward; a short setup needs both to slope downward.
2. **CCI exhaustion filter.** A second candle subscription (30-minute by default) powers the `CciPeriod` CCI. Long trades only fire when CCI dips below `BuyCciLevel` (−200), while shorts require CCI above `SellCciLevel` (+200).
3. **Pyramid limit.** The aggregated net position cannot exceed `MaxTradesPerSymbol × volume`. When a fresh signal appears, the strategy closes any opposite exposure and then adds up to the allowed capacity in the signal direction.
4. **Exits.** Positions are closed either when the trend EMA loses its slope advantage (mirroring the MQL condition `MA <= MAprevious`) or after price travels `StopPoints` instrument points in favour of the position.

## Risk management

- **Risk-based volume.** Order size defaults to `Portfolio.CurrentValue × MaximumRisk ÷ price`. When equity information is missing, the engine falls back to the `Lots` parameter (or the strategy `Volume`).
- **Loss streak adjustment.** After two or more consecutive losing trades the new order size is reduced by `volume × losses ÷ DecreaseFactor`, matching the MQL helper `LotsOptimized()`.
- **Normalization.** The final volume is aligned with the instrument’s `VolumeStep`, bounded by `MinVolume`, and clipped by `Security.MaxVolume` when provided.

## Parameters

| Name | Default | Description |
|------|---------|-------------|
| `MaxTradesPerSymbol` | `3` | Maximum number of aggregated entries allowed per direction. |
| `Lots` | `1` | Fallback fixed lot size when portfolio equity is unavailable. |
| `MaximumRisk` | `0.05` | Fraction of equity used to size each new trade. |
| `DecreaseFactor` | `6` | Divider that shrinks the next order after consecutive losses. |
| `StopPoints` | `10` | Profit target distance in instrument points. Set to `0` to disable. |
| `MaPeriod` | `85` | EMA period applied to 15-minute candles for the trend slope check. |
| `CloseMaPeriod` | `70` | EMA period applied to 15-minute candles for the close smoothing filter. |
| `CciPeriod` | `12` | Lookback used for the 30-minute CCI filter. |
| `BuyCciLevel` | `-200` | Oversold CCI threshold required for long entries. |
| `SellCciLevel` | `200` | Overbought CCI threshold required for short entries. |
| `MinVolume` | `0.1` | Minimum volume allowed after normalization. |
| `TrendCandleType` | `15m` | Candle type used for EMA calculations (median price). |
| `CciCandleType` | `30m` | Candle type used for the CCI filter. |

## Notes and limitations

- StockSharp operates in netting mode, so multiple MT4 tickets are represented as a single aggregated position. The `MaxTradesPerSymbol` guard therefore limits the net exposure instead of counting individual orders.
- The original EA relied on `AccountFreeMargin` for sizing. This port approximates it with `Portfolio.CurrentValue`; adjust `MaximumRisk` or `Lots` to fit your broker’s contract specifications.
- Ensure both candle subscriptions are enabled in the data source, otherwise the EMA or CCI filters will never form and the strategy will stay idle.
