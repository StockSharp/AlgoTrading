# Gridder EA Strategy (Ported from MQL4)

## Overview
The original GridderEA is a multi-symbol grid trading expert advisor designed for MetaTrader 4. This StockSharp port keeps the core concepts—progressive spacing, adaptive lot sizing, basket take-profit, and emergency hedging—while focusing on a single instrument managed by the hosting strategy. The strategy subscribes to a configurable candle stream, watches finished bars, and opens averaging trades when price moves away from the last reference level by a distance defined in pips.

## Trading Logic
1. **Grid progression** – A base step (in pips) defines the minimum price movement required before placing a new trade. Each additional order can scale this step geometrically or exponentially to spread the grid when volatility increases.
2. **Lot progression** – The first order uses the initial volume. Subsequent orders multiply the previous volume according to the configured lot progression mode (static, geometric, or exponential).
3. **Basket targets** – Unrealized profit and loss are measured in account currency by combining the price deviation of every open trade with the instrument step value. When total profit exceeds the target profit per lot, all positions are closed. Likewise, a target loss per lot can liquidate the basket as a protective stop.
4. **Emergency mode** – When the number of trades on one side reaches the emergency trigger, the strategy optionally opens a hedge trade sized as a fraction of the accumulated volume. This imitates the “Emergency Mode” from the MQL version and helps cap drawdowns.
5. **Position protection** – `StartProtection()` is invoked during start-up to ensure the base strategy monitors unexpected position changes and re-synchronizes with the exchange state.

The StockSharp implementation avoids manipulating large historical collections and processes only finished candles, mirroring the original expert’s behaviour on completed bars.

## Parameters
| Parameter | Description |
|-----------|-------------|
| **Initial Volume** | Volume for the very first grid order. |
| **Volume Multiplier** | Factor applied to calculate the next order volume when lot progression is geometric or exponential. |
| **Grid Step (pips)** | Base distance (in pips) between successive entries. |
| **Step Multiplier** | Scaling factor for the grid spacing when step progression is geometric or exponential. |
| **Target Profit / Lot** | Unrealized profit target expressed per lot. Reaching this target closes every open trade. |
| **Target Loss / Lot** | Unrealized loss threshold per lot. When reached, all trades are closed to contain drawdown. |
| **Max Orders Per Side** | Limits the number of averaging trades allowed on each side of the market. `0` disables the limit. |
| **Allow Long / Allow Short** | Enable or disable buying/selling legs independently. |
| **Step Mode** | Determines how the step grows: static, geometric, or exponential. |
| **Lot Mode** | Determines how the order volume grows: static, geometric, or exponential. |
| **Use Emergency Mode** | Enables the hedge logic that protects against oversized baskets. |
| **Emergency Trigger** | Number of orders on one side that activates the hedge. |
| **Hedge Volume Factor** | Fraction of the total side volume placed as the hedge order when emergency mode triggers. |
| **Candle Type** | Time frame of the candle subscription used for grid calculations. |

## Differences from the Original EA
- The port manages a single security at a time; attach multiple strategy instances to trade several instruments, replicating the multi-symbol behaviour of the MQL expert.
- Screen panels and chart annotations from MetaTrader are not reproduced; use StockSharp chart areas to visualise candles and own trades if desired.
- Money-management presets and detailed partial close profiles are simplified into the unified basket profit/loss logic.

## Usage Notes
1. Configure the desired candle type, volume, and grid spacing in the constructor parameters (via the UI or optimisation interface).
2. Start the strategy once the security is connected to a live or simulated board. The strategy automatically subscribes to the selected candles.
3. Monitor the emergency trigger and hedge factor to adjust the aggressiveness of the recovery phase. A higher hedge factor brings the net position back to neutral faster but reduces profitability.
4. Combine with StockSharp risk controls (portfolio protection, max position watcher, etc.) for additional safety.

## Emergency Hedge Example
Assume the strategy has opened five averaging buy orders at progressively larger volumes. If the emergency trigger is set to five and the hedge volume factor is 0.5, the moment the fifth buy fills the strategy will send an automatic market sell sized at half of the total long volume. This mirrors the MQL logic that partially locks the basket and waits for a mean-reversion exit.

## Optimisation Tips
- Optimise **Grid Step (pips)** and **Volume Multiplier** together; small steps require conservative multipliers to avoid runaway exposure.
- Use **Target Profit / Lot** to translate MetaTrader’s dollar targets into the StockSharp environment without relying on closed trade history.
- Tune **Emergency Trigger** and **Hedge Volume Factor** according to the volatility of the traded instrument. Higher volatility usually benefits from earlier hedging.

## Safety Recommendations
- Test extensively in the simulator before deploying to production.
- Monitor broker-specific contract sizes to ensure the rounded volume matches the actual lot granularity.
- Combine with stop-out rules (for example, via the hosting robot) to prevent catastrophic loss during trend markets where grids can accumulate large positions.
