# Fundamental Trader DailyFX Strategy

## Overview
The strategy is a StockSharp port of the **Fundamental Trader DailyFX v0.04** MetaTrader expert. It reacts to macroeconomic releases published in the DailyFX CSV calendar and opens a position when the published number deviates from expectations. The conversion keeps the event-driven logic, adds safety checks for the multi-asset environment, and preserves the configurable lot sizing table from the original robot.

## Trading Logic
- On every timer tick the strategy parses the configured DailyFX CSV file.
- Events with *High* importance and timestamps within the configurable time window (`WaitingMinutes`) are considered.
- When both Actual and Forecast values are present, the relative deviation `|Actual - Forecast| / |Forecast|` is computed. If Forecast is missing, the strategy falls back to a comparison with the Previous value.
- The deviation chooses a volume level (18 steps like in the EA). Positive surprises buy the currency when it is the base of the selected symbol and sell when it is the quote. Negative surprises invert the direction.
- Stop-loss and take-profit levels are derived from the pip risk (`RiskPips`) and reward multiplier using the latest bid/ask quotes.
- Protective exits monitor Level1 data. If enabled, positions are also closed after a fixed holding time (`EnableCloseByTime` / `CloseAfterMinutes`).

## Parameters
- **Calendar File** (`CalendarFilePath`): Path to the DailyFX CSV calendar. The strategy expects the user to download or generate this file manually.
- **Waiting Minutes** (`WaitingMinutes`): Minutes before/after the event time during which trading is allowed.
- **Enable Timed Exit** (`EnableCloseByTime`): Close positions after the configured duration.
- **Timed Exit Minutes** (`CloseAfterMinutes`): Holding time used when timed exit is enabled.
- **Risk (pips)** (`RiskPips`): Stop-loss distance expressed in pips.
- **Reward Multiplier** (`RewardMultiplier`): Take-profit distance multiplier relative to the stop-loss.
- **Timer Frequency (sec)** (`TimerFrequencySeconds`): Frequency of CSV polling and timed-exit checks.
- **Calendar TZ Offset (h)** (`CalendarTimeZoneOffsetHours`): Hour offset applied to timestamps read from the CSV.
- **Currency Map** (`CurrencyMap`): Semicolon-separated list mapping calendar currency codes to tradable symbols, e.g. `EUR=EURUSD;USD=EURUSD;JPY=USDJPY`.
- **VolumeLevel1 .. VolumeLevel18**: Lot sizes used for each deviation bucket. Defaults match the original EA and can be optimised.

## Implementation Notes
- CSV parsing uses a lightweight in-code reader that supports quoted fields and mirrors the EA behaviour without additional dependencies.
- Quotes are taken from Level1 subscriptions per instrument, allowing the strategy to work with multiple FX pairs simultaneously.
- Volumes are aligned to the instrument's `VolumeMin`/`VolumeStep` before placing orders.
- Protective stop and take-profit management is performed inside the strategy by monitoring bid/ask prices, so there is no reliance on server-side bracket orders.
- The original DLL-based `StringToDouble` helper is replaced with robust string sanitising that handles prefixes, thousands separators and parentheses.

## Usage
1. Download the DailyFX calendar CSV and update it as required (for example by scheduling an external downloader).
2. Configure `CurrencyMap` so that every calendar currency points to a symbol available in your StockSharp connection.
3. Adjust risk, reward and lot parameters according to the trading plan.
4. Start the strategy – it will subscribe to Level1 data, poll the CSV at the configured frequency and open/close positions based on releases.

## Differences from the Original EA
- No automatic HTTP download: the CSV file is expected to be provided by the user or by an external process.
- Uses Level1 quotes to emulate MetaTrader's `MarketInfo` calls and to implement stop/target handling.
- Volume rounding is compliant with instrument limits, avoiding the invalid lot issues fixed in the original EA.
- The time-based exit is implemented via the strategy timer instead of the MQL trade loop.
- Detailed logging is provided for mapping issues (missing securities or quotes) which simplifies troubleshooting in a multi-asset environment.
