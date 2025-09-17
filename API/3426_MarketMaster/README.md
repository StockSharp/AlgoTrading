# Market Master Strategy

## Overview

`MarketMasterStrategy` is a high-level StockSharp conversion of the MetaTrader 4 expert advisor "Market Master" (`MQL/31326/MarketMaster EN.mq4`). The original bot combined a rich indicator stack with intricate money management rules, news avoidance and multi-stage order pyramids. The C# port focuses on the deterministic technical core so that it can run on StockSharp's event-driven engine without any external web services. All indicator decisions are calculated on the trading timeframe through a single candle subscription, in line with the repository guidelines.

## Core Indicators

The strategy binds the following StockSharp indicators to the trading candle series:

- **AverageTrueRange (ATR)** – two instances are maintained. The first tracks primary entry conditions, the second mirrors the MT4 "hedge" ATR that was used for recovery positions.
- **MoneyFlowIndex (MFI)** – measures volume-adjusted price flow to detect accumulation or distribution swings.
- **BullsPower / BearsPower** – replicate the MT4 `iBullsPower` and `iBearsPower` filters that required bullish/bearish dominance before taking trades.
- **StochasticOscillator** – delivers `%K` and `%D` lines. The conversion honours the original oscillator lengths and allows the user to toggle the filter on or off.
- **ParabolicSar** – two timeframes were used in MetaTrader. The StockSharp port keeps two independent SAR indicators (primary and confirmation) whose steps mirror the expert advisor inputs.

All indicators are warmed up automatically by StockSharp. The strategy does not access indicator history through `GetValue` – instead it stores the previous values inside private fields (`_prevAtr`, `_prevMfi`, `_prevStochasticMain`, etc.) as required by the conversion rules.

## Signal Logic

The MQL expert defined two main entry families ("ZERO" and "MA"). They share identical ATR/MFI/Bulls/Bears filters but differ in the oscillator confirmation. The StockSharp version exposes the richer "MA" branch because it is the most restrictive and therefore closest to real trading conditions. A long signal is confirmed when all of the following are true on a finished candle:

1. ATR is rising relative to the previous candle (either the primary ATR or the hedge ATR depending on whether a position already exists).
2. MFI is rising and Bears Power is positive, signalling bullish pressure.
3. The Stochastic oscillator is enabled and `%K` is above `%D`, trending upwards, while `%K` remains below the configurable overbought ceiling (`StochasticBuyLevel`).
4. Parabolic SAR filters are enabled and the candle closes above both SAR values.
5. Current candle volume meets the configured threshold (`MinVolume` or `MinHedgeVolume`).

Short signals mirror the long logic with decreasing MFI, negative Bulls Power, `%K` below `%D` and SAR values above price. Volume checks prevent trading during thin markets, replicating the `iVolume` calls from MT4.

## Position Management

- **Auto volume** – the original EA offered a balance-based position sizing block. `CalculateBaseVolume` follows the same spirit by scaling the order volume with `RiskMultiplier` while honouring the instrument's `VolumeStep`, `MinVolume` and `MaxVolume` constraints.
- **Pyramiding** – when `AllowSameSignalEntries` is `true`, additional orders reuse the base volume multiplied by `VolumeMultiplier`. Because StockSharp strategies work with net positions, pyramiding increases the net long or net short exposure rather than opening parallel tickets.
- **Opposite signals** – `AllowOppositeEntries` controls whether a detected reversal immediately closes the current position and optionally opens a trade in the new direction. When disabled the strategy exits but waits for a fresh signal before re-entering, mimicking the "No opposite signal" toggle in the MT4 interface.
- **Stop-loss** – the MT4 input `StopLoss` is exposed as `StopLossPoints`. If the instrument provides a `PriceStep`, the value is converted into StockSharp protective orders through `StartProtection`.
- **Trading hours** – `UseTradingWindow`, `TradingStart`, `TradingEnd`, `UseTradingBreak`, `BreakStart` and `BreakEnd` reproduce the opening window and intraday pause from the source expert. Time comparisons are performed in the exchange time zone carried by the incoming candle messages.

## Differences versus MetaTrader Version

- **News filters** – the MT4 robot downloaded economic calendar data from Investing.com and DailyFX. The conversion omits all network calls and replaces them with manual control over the trading window. For news-sensitive behaviour, adjust the timing parameters or pause the strategy externally.
- **Order history checks** – functions such as `OrdersHistoryTotal()` and "open again" logic were tightly coupled to MetaTrader's ticket model. StockSharp works with a net position, so the port simply allows re-entry when the direction filter becomes valid again.
- **Recovery orders** – the original code managed multiple Magic Numbers and comment labels. The port keeps the multiplier logic (`VolumeMultiplier`) but each additional order modifies the single net position.
- **Trailing stop** – MetaTrader's `TrailingStop`/`TrailingStep` block relied on asynchronous order modification. StockSharp users can extend the strategy by subscribing to `PositionChanged` events or by enabling trailing options in `StartProtection`, but the baseline conversion focuses on signal parity.

## Parameters

| Property | Default | Description |
| --- | --- | --- |
| `OrderVolume` | `1` | Base order size when auto-volume is disabled. |
| `UseAutoVolume` | `true` | Enable risk-based volume scaling. |
| `RiskMultiplier` | `10` | Percentage of portfolio balance used in the auto-volume calculation (mirrors `Risk_Multiplier`). |
| `VolumeMultiplier` | `2` | Pyramiding factor for additional entries (`KLot`). |
| `MinVolume` | `3000` | Minimum candle volume for the first entry (`MinVol`). |
| `MinHedgeVolume` | `3000` | Volume threshold for add-on trades (`MinVolH`). |
| `AtrPeriod` / `AtrHedgePeriod` | `14` | ATR lengths for the base and hedge filters. |
| `MfiPeriod` | `14` | MFI period. |
| `BullBearPeriod` | `14` | Bulls/Bears Power period. |
| `StochasticKPeriod` / `StochasticDPeriod` / `StochasticSlowing` | `5 / 3 / 3` | Stochastic oscillator configuration. |
| `StochasticBuyLevel` / `StochasticSellLevel` | `60 / 40` | Oscillator thresholds (`StoBuy` and `StoSell`). |
| `UseStochasticFilter`, `UsePsarFilter`, `UsePsarConfirmation` | `true` | Toggles for indicator-based confirmations. |
| `PsarStep` / `PsarMaxStep` / `PsarConfirmStep` / `PsarConfirmMaxStep` | `0.02 / 0.2 / 0.02 / 0.2` | SAR accelerations and caps. |
| `AllowSameSignalEntries` | `false` | Enable pyramiding on identical signals. |
| `AllowOppositeEntries` | `true` | Allow immediate reversal trades. |
| `UseTradingWindow` | `false` | Restrict trading to a time interval. |
| `TradingStart` / `TradingEnd` | `06:00 / 18:00` | Daily trading window. |
| `UseTradingBreak` | `false` | Enable a short intraday break. |
| `BreakStart` / `BreakEnd` | `06:00:01 / 06:00:02` | Break boundaries (match MT4 defaults). |
| `StopLossPoints` | `0` | Optional protective stop in instrument points. |
| `CandleType` | `15m TimeFrame` | Candle series used for all indicators. |

## Usage Notes

1. Attach the strategy to a security and portfolio in StockSharp Designer or in code, then start it during warm-up hours to allow all indicators to form.
2. If you require multi-timeframe confirmation, adjust `CandleType` and the SAR settings accordingly. The strategy subscribes to a single candle feed and binds every indicator through `Bind`, so no manual indicator registration is necessary.
3. Use StockSharp logging (`LogInfo`, `LogWarning`) for debugging if you extend the code. The conversion keeps the internal state management simple so that additional modules (e.g., trailing protection) can be plugged in easily.
4. The strategy is net-position based. If you plan to model individual ticket behaviour similar to MetaTrader, wrap the strategy inside a multi-security router that tracks synthetic tickets.

## Extending the Port

- Implement custom exit logic by overriding `OnNewMyTrade` or subscribing to `PositionChanged`.
- Add economic calendar integration by introducing an external component that toggles `UseTradingWindow` or calls `Stop()` when high-impact events approach.
- For signal visualisation, call `CreateChartArea()` and `DrawIndicator()` in `OnStarted` – the conversion leaves those hooks empty for clarity.

The code is fully compliant with the repository guidelines: it uses tab indentation, high-level `Bind` subscriptions, avoids indicator back-references and exposes all configurable inputs via `StrategyParam` objects.
