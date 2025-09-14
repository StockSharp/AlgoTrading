# Personal Assistant Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Personal Assistant strategy is a manual trading helper built on the StockSharp API. It mirrors the original MetaTrader script by displaying key account metrics and providing simple methods to open or close positions. The strategy does not implement automatic entry rules; instead, it focuses on quick manual actions and real-time information for discretionary trading.

It subscribes to a user-defined candle series only to trigger periodic status reports. When each candle is completed, the strategy logs current position data, profit, volume, tick value and spread. This makes it easy to monitor the trading environment without writing additional code.

Manual operations are exposed through public methods:

- `ManualBuy()` – open a long position using the configured volume.
- `ManualSell()` – open a short position using the configured volume.
- `CloseAllPositions()` – close any existing position at market.
- `IncreaseLot()` – raise the trading volume by 0.01.
- `DecreaseLot()` – lower the trading volume by 0.01.

The strategy is useful as a template for learning StockSharp or for traders who prefer semi-automated operation with minimal code.

## Details

- **Entry Criteria**: Manual only.
- **Long/Short**: Both directions.
- **Exit Criteria**: Manual closure via `CloseAllPositions`.
- **Stops**: None by default.
- **Default Values**:
  - `Id` = 3900
  - `LotVolume` = 0.01
  - `Slippage` = 2
  - `CandleType` = 1-minute time frame
- **Filters**:
  - Category: Utility
  - Direction: Both
  - Indicators: None
  - Stops: No
  - Complexity: Simple
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Manual

