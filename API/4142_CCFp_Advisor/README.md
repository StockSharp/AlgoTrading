# CCFp Advisor Strategy

This strategy replicates the MetaTrader expert advisor **CCFp** (Currency Comparative Force) that trades a portfolio of major FX pairs. It ranks eight currencies by comparing fast and slow composite moving averages for each USD pair, then opens positions in the symbols that correspond to the strongest and weakest currencies. Only finished candles are used, and every calculation is performed with high-level StockSharp indicators.

## How it works

1. Subscribe to the same timeframe candles for seven major USD crosses (EURUSD, GBPUSD, AUDUSD, NZDUSD, USDCHF, USDJPY, USDCAD).
2. For each pair maintain two composite moving averages. The indicator multiplies the base `FastPeriod` and `SlowPeriod` by a set of multipliers derived from the chart timeframe (as the original MQL `ma()` helper does) and sums the resulting moving averages.
3. Convert the per-symbol fast/slow aggregates into currency-strength values using the original cross-rate formulas. The latest completed bar produces the “current” snapshot; the previous bar is used as the reference snapshot.
4. The currency with the highest strength becomes the **strong** leader; the lowest becomes the **weak** laggard. The strategy opens one position in the corresponding major pair if that currency has just taken the lead compared with the previous bar (the `MAX1`/`MIN1` filters from MQL).
5. If the currently held strong/weak currency stops being the top/bottom performer, the matching position is closed immediately. Protective stops in pips are simulated on candle data using the last close price.

## Parameters

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `MaType` | `MovingAverageMode` | `Exponential` | Moving-average algorithm applied to every composite moving average (Simple, Exponential, Smoothed, Weighted). |
| `PriceMode` | `CandlePrice` | `Close` | Candle price used as the input value (replicates the MQL “Applied Price” option). |
| `FastPeriod` | `int` | `3` | Base period of the fast composite moving average. |
| `SlowPeriod` | `int` | `5` | Base period of the slow composite moving average. |
| `StopLossPips` | `decimal` | `200` | Stop-loss distance expressed in pips from the entry price. Set to `0` to disable the simulated stop. |
| `TradeVolume` | `decimal` | `0.1` | Fixed lot size submitted with every market order. |
| `CandleType` | `DataType` | `H1` | Candle type/timeframe used to build the indicator. The multipliers follow the original MQL switch logic for M1/M5/M15/M30/H1/H4/D1/W1/MN. |
| `EurUsdSecurity` | `Security` | – | Instrument used when EUR is strongest/weakest. |
| `GbpUsdSecurity` | `Security` | – | Instrument used when GBP is strongest/weakest. |
| `AudUsdSecurity` | `Security` | – | Instrument used when AUD is strongest/weakest. |
| `NzdUsdSecurity` | `Security` | – | Instrument used when NZD is strongest/weakest. |
| `UsdChfSecurity` | `Security` | – | Instrument used when CHF is strongest/weakest (traded via USDCHF). |
| `UsdJpySecurity` | `Security` | – | Instrument used when JPY is strongest/weakest (traded via USDJPY). |
| `UsdCadSecurity` | `Security` | – | Instrument used when CAD is strongest/weakest (traded via USDCAD). |

All seven securities must be assigned before the strategy starts. USD itself is evaluated in the strength table but is never traded directly, mirroring the original EA.

## Conversion notes

- The composite moving-average helper reproduces the falling-through `switch` logic of the MQL `ma()` routine, so each timeframe uses the same multiplier set as the source indicator.
- Currency-strength arrays reuse the exact algebraic formulas from the MetaTrader custom indicator, ensuring the rankings match when the same inputs are provided.
- Protective stops are managed inside the strategy by watching candle highs and lows, replacing the individual stop orders that the MQL EA submits for every trade.
- Positions are tracked per role (strong vs. weak). Whenever the leader changes, the old position is flattened before a new one is opened, keeping the behaviour consistent with the `magicMAX`/`magicMIN` management in MQL.

## Usage

1. Add the strategy to a connector that provides all required Forex majors and assign the corresponding `Security` parameters.
2. Configure the desired candle timeframe and verify that the data source supports synchronized bars for all pairs.
3. Adjust the moving-average lengths, price mode, and stop distance if you want to mimic a different CCFp indicator setup.
4. Start the strategy; it will automatically subscribe to each pair, compute currency strengths, and manage one “strong” and one “weak” position at a time.
