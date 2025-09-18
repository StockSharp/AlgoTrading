# MultiTrader Currency Strength Strategy (3253)

## Overview
This strategy is a high-level StockSharp port of the public "MultiTrader" MQL panel (code base #24786). The original Expert Advisor was a discretionary dashboard that displayed the relative strength of the eight major currencies, triggered visual/audio alerts when a currency became extremely strong or weak, and suggested which Forex pair to trade. The StockSharp version automates the same analytical workflow and optionally executes trades on the strongest-vs-weakest pair.

The logic calculates a % position of each symbol's close within its current candle range. Averaging the relevant crosses produces a strength score for AUD, CAD, CHF, EUR, GBP, JPY, NZD, and USD. When one currency climbs above the configurable buy threshold and another drops below the sell threshold, the strategy recommends the pair built from those currencies. If the pair exists in the configured universe, the strategy can automatically place a market order in that direction.

## Currency strength model
The percent score for a symbol is computed as:

```
percent = 100 * (Close - Low) / (High - Low)
```

Each currency strength is derived from seven crosses, mirroring the MQL implementation. A `100 - percent` inversion is applied whenever the currency appears as the quote currency in the pair:

| Currency | Components |
| --- | --- |
| AUD | AUDJPY, AUDNZD, AUDUSD, 100-EURAUD, 100-GBPAUD, AUDCHF, AUDCAD |
| CAD | CADJPY, 100-NZDCAD, 100-USDCAD, 100-EURCAD, 100-GBPCAD, 100-AUDCAD, CADCHF |
| CHF | CHFJPY, 100-NZDCHF, 100-USDCHF, 100-EURCHF, 100-GBPCHF, 100-AUDCHF, 100-CADCHF |
| EUR | EURJPY, EURNZD, EURUSD, EURCAD, EURGBP, EURAUD, EURCHF |
| GBP | GBPJPY, GBPNZD, GBPUSD, GBPCAD, 100-EURGBP, GBPAUD, GBPCHF |
| JPY | 100-AUDJPY, 100-CHFJPY, 100-CADJPY, 100-EURJPY, 100-GBPJPY, 100-NZDJPY, 100-USDJPY |
| NZD | NZDJPY, 100-GBPNZD, NZDUSD, NZDCAD, 100-EURNZD, 100-AUDNZD, NZDCHF |
| USD | 100-AUDUSD, USDCHF, USDCAD, 100-EURUSD, 100-GBPUSD, USDJPY, 100-NZDUSD |

The strategy stores the latest completed candle per pair, keeps the most recent percent, and refreshes the currency strengths after every update.

## Trading and alerts
1. When all eight currencies have valid data, the strategy logs a snapshot (strongest to weakest).
2. If the strongest value is **≥ BuyLevel** and the weakest value is **≤ SellLevel**, a trading suggestion is generated.
3. The strategy attempts to find the direct pair (strong currency as base, weak currency as quote). If it does not exist, it checks the inverse orientation and finally falls back to pairs involving USD.
4. The detected pair and direction are logged. If `EnableAutoTrading` is `true` and `OrderVolume` is positive, the strategy issues a market order in the suggested direction. Opposite positions are flattened automatically by increasing the order size.

Signals are throttled by remembering the last suggested pair and side, preventing duplicate alerts until the market leaves the threshold zone.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `Universe` | List of `Security` objects representing the FX pairs (28 majors recommended). | Required |
| `CandleType` | Candle specification used for the calculations (Daily, Weekly, Monthly, etc.). | Daily candles |
| `BuyLevel` | Threshold above which a currency is treated as overbought. | 90 |
| `SellLevel` | Threshold below which a currency is treated as oversold. | 10 |
| `EnableAutoTrading` | Enable or disable automatic order placement. | false |
| `OrderVolume` | Volume to submit with market orders when auto trading is enabled. | 1 |
| `SymbolPrefix` | Optional prefix used by the broker/exchange (e.g., `m.`). | "" |
| `SymbolSuffix` | Optional suffix used by the broker/exchange (e.g., `.FX`). | "" |

## Configuration steps
1. **Universe setup.** Add the 28 major Forex crosses to the strategy universe. Codes should match the canonical pair names (e.g., `EURUSD`). Use `SymbolPrefix`/`SymbolSuffix` if your broker adds decorations.
2. **Timeframe selection.** Choose the desired `CandleType`. Daily, weekly, and monthly candles reproduce the original panel modes.
3. **Threshold tuning.** Adjust `BuyLevel`/`SellLevel` to control how extreme the strength needs to be before a signal is generated.
4. **Auto trading (optional).** Set `EnableAutoTrading` to true and define `OrderVolume`. Leave the flag false to only receive informational logs.

## Migration notes
- The entire GUI layer of the original MQL panel is intentionally omitted. All output is available through the strategy log.
- Alerts are emitted as `LogInfo` entries; push/email/desktop notifications were not ported.
- Auto stop-loss/target calculations from the MQL version are not supported; traders should manage risk using StockSharp protection modules or external risk controls.
- The DES-based licensing helper embedded in the MQL script was removed.

## Recommended usage
- Deploy the strategy inside a connector session that provides real-time and historical candles for all relevant pairs.
- Combine with a chart widget to visualize the suggested pair and monitor the underlying candle series.
- Use StockSharp's `StartProtection` parameters or separate risk strategies to enforce global stops/targets.

## Testing considerations
- Verify that your data source delivers completed candles for the selected timeframe; the strategy ignores unfinished bars.
- If some pairs are missing from the universe, the corresponding currency cannot be calculated and no signal will be produced.
- When evaluating historical performance, ensure that the universe remains static throughout the backtest to avoid strength gaps.
