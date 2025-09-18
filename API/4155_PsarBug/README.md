# Psar Bug Strategy

## Overview
The **Psar Bug Strategy** is a direct port of the MetaTrader 4 expert advisor `pSAR_bug.mq4`. It reacts to the very first Parabolic SAR dot that appears on the opposite side of price and immediately reverses the position. The StockSharp implementation subscribes to candles, evaluates completed bars only, and uses the high-level API to place market orders and manage protective stops.

## Trading Logic
- Calculate the Parabolic SAR with an acceleration step of `0.02` and a maximum acceleration of `0.2` (both configurable).
- Wait for a finished candle where the Parabolic SAR value flips relative to the close:
  - **Long entry**: the current SAR value is below the closing price while the previous SAR value was above the previous close.
  - **Short entry**: the current SAR value is above the closing price while the previous SAR value was below the previous close.
- Reverse existing exposure on every signal. When a buy signal appears any open short position is flattened and replaced with a long position of the configured size. The opposite applies to sell signals.
- Apply fixed stop-loss and take-profit distances expressed in instrument price steps. Protection is implemented with `StartProtection` so the risk parameters automatically attach to every new position.

## Parameters
| Name | Description |
| --- | --- |
| `TradeVolume` | Order volume in lots used for entries. The default value is `0.1` lots. |
| `StopLossPoints` | Distance from the entry price to the stop-loss expressed in price steps. Mirrors the MetaTrader `StopLoss` input. |
| `TakeProfitPoints` | Distance from the entry price to the take-profit expressed in price steps. Mirrors the MetaTrader `TakeProfit` input. |
| `SarAccelerationStep` | Initial acceleration factor of the Parabolic SAR indicator. |
| `SarAccelerationMax` | Maximum acceleration factor for the Parabolic SAR calculation. |
| `CandleType` | Candle data type (timeframe) used for the indicator calculations. By default the strategy works on 15-minute candles. |

## Notes on the Conversion
- The original expert directly references the current chart symbol and timeframe. The StockSharp version exposes the candle type as a parameter so the timeframe can be changed without recompiling.
- Protective stops are represented as absolute price offsets. They are initialized once at startup and managed automatically by the platform.
- Order management relies on netting logic: buying `Volume + |Position|` lots both closes the previous short and opens the new long, reproducing the MetaTrader behaviour of closing before opening in the opposite direction.

## Usage
1. Configure the desired security, timeframe (`CandleType`), and risk parameters inside StockSharp Designer or Backtester.
2. Ensure market data is available and start the strategy. Signals are evaluated on finished candles only.
3. Monitor positions and performance through the standard StockSharp tooling. The charts display candles, the Parabolic SAR indicator, and executed trades for visual validation of the reversal signals.
