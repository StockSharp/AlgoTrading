# VR Overturn Strategy

## Overview
- Recreates the "VR---Overturn" MetaTrader expert using StockSharp high-level APIs.
- Keeps only one open position at a time and immediately evaluates the next trade once the previous one closes.
- Built for discretionary traders who want automatic position reversal with martingale or anti-martingale sizing.

## Trading Logic
1. **Initial position** – the strategy opens the first trade in the configured direction (`FirstPositionDirection`) with the base volume (`BaseVolume`).
2. **Stop loss / take profit** – protective exit orders are attached automatically using `StopLossPips` and `TakeProfitPips`. The engine converts pips to absolute price offsets by analysing the security price step (3 and 5-digit instruments get the 10x adjustment just like in the original expert).
3. **Position close processing** – when a position is closed by either protective order the strategy records:
   - Side of the closed trade (long or short).
   - Filled volume.
   - Realized PnL (difference between entry and exit price).
4. **Next entry sizing** – the stored result decides the side and the lot size of the next order.
   - Winning trades keep the same direction, losing trades flip direction.
   - Martingale mode multiplies the position size after a loss and resets to the base volume after a win.
   - Anti-martingale mode multiplies the position size after a win and resets to the base volume after a loss.
5. **Lot rounding** – the calculated size is trimmed to the nearest volume step of the instrument before a market order is sent.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `FirstPositionDirection` | Direction of the very first trade (Buy/Sell). | Buy |
| `Mode` | Sizing regime: Martingale (increase after losses) or AntiMartingale (increase after wins). | Martingale |
| `BaseVolume` | Initial position volume. Used when a sequence resets. | 0.1 |
| `StopLossPips` | Distance to the stop loss in pips. | 30 |
| `TakeProfitPips` | Distance to the take profit in pips. | 90 |
| `LotMultiplier` | Multiplier applied during the expansion step (after loss for martingale, after win for anti-martingale). | 1.6 |

## Risk Management
- Uses `StartProtection` to attach both stop-loss and take-profit orders for every entry.
- Stop and target distances are absolute price offsets derived from the configured pip values.
- No additional trailing logic is applied, so risk is entirely controlled by the protective orders and position reversal rules.

## Operational Notes
- The strategy does not rely on candles or indicators; it reacts purely to trade confirmations (`OnOwnTradeReceived`).
- If a protective order partially fills, the strategy accumulates the remaining amount until the position is flat before acting again.
- Commission and swap values are not available in StockSharp trades, so the profit comparison uses price difference only. Consider widening stops or multipliers if your venue charges significant fees.
- Works on any instrument that provides price step and volume step metadata; verify both before deploying to production.
