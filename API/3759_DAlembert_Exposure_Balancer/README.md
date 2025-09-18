# D'Alembert Exposure Balancer Strategy

## Overview

The strategy is a StockSharp port of the MetaTrader "E04LC07" d'Alembert multicurrency system. It trades a basket of three
currency pairs (EURUSD, EURGBP, GBPUSD) and manages the net exposure between their shared currencies. A simplified Heikin-Ashi
trend filter is used to decide when to join or exit the market, while a d'Alembert staking model adjusts the order size after
wins and losses.

The algorithm opens positions in the direction specified by the original MQL template: long EURUSD, short EURGBP and short
GBPUSD. The `InvertSignals` switch allows mirroring every bias. When a trade closes with profit, the next stake is decreased; a
loss increases the stake. Exposure and position caps prevent the basket from becoming unbalanced.

## Trading Logic

1. Subscribe to the configured candle type for each pair and calculate Heikin-Ashi values.
2. Wait for the configured cooldown before taking new decisions for a symbol.
3. Open a position when the Heikin-Ashi bias matches the default direction and both position and exposure limits allow it.
4. Manage an active long position by closing it when:
   - The Heikin-Ashi candle flips bearish,
   - The configured take profit ratio is reached,
   - The configured loss recovery threshold is broken.
5. Manage an active short position using the symmetric rules.
6. After every position is closed, update the d'Alembert level: decrease it after a win, increase it after a loss.
7. Keep a live exposure map for EUR, GBP and USD to ensure every currency stays below the allowed absolute exposure.

## Parameters

| Name | Description |
| ---- | ----------- |
| `EurUsd`, `EurGbp`, `GbpUsd` | Securities participating in the basket. |
| `CandleType` | Candle resolution used for the Heikin-Ashi filter. |
| `InvertSignals` | Flip the default hedge directions. |
| `BaseVolume` | Minimal lot size used when calculating the trade volume. |
| `LotMultiplier` | Multiplier applied to `BaseVolume`. |
| `StartLevel` | Initial d'Alembert staking level (minimum 1). |
| `MaxPositionPerSymbol` | Maximum absolute net volume allowed for a single symbol. |
| `MaxExposurePerCurrency` | Maximum absolute volume allowed per individual currency. |
| `ManageTakeProfit` | Enable automatic take-profit management. |
| `ManageLoss` | Enable automatic loss recovery exits. |
| `TakeProfitPercent` | Profit target expressed as a fraction of the entry price. |
| `LossPercent` | Allowed adverse excursion before closing the position. |
| `CooldownSeconds` | Pause between decisions for the same symbol. |

## Notes

- The strategy assumes the security codes follow the standard six-character FX format (e.g., `EURUSD`).
- Exposure calculations are performed in raw lot units for simplicity; portfolio conversion to a single currency can be added if
  required.
- When the basket is inverted the volume management and exposure logic remain unchanged.
- The Python version of the strategy is intentionally omitted per repository guidelines.
