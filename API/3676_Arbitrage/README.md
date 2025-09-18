# Arbitrage Strategy

## Overview
The **Arbitrage Strategy** replicates the MetaTrader expert advisor `Arbitrage.mq5`. It watches the EURUSD, GBPUSD, and EURGBP currency pairs and looks for temporary mispricings between the synthetic EURGBP rate (constructed from EURUSD and GBPUSD) and the actual EURGBP quote. When the divergence is large enough to pay for the commissions and current spreads of the three legs, the strategy opens a fully hedged basket of market orders to capture the imbalance.

## Trading Logic
1. Subscribe to Level 1 quotes for the three pairs and keep the best bid/ask prices in memory.
2. Compute two synthetic prices on every quote update:
   - `syntheticSell = EURUSD_ask / GBPUSD_bid`
   - `syntheticBuy = EURUSD_bid / GBPUSD_ask`
3. Estimate transaction costs:
   - Sum of the three spreads (`ask - bid`).
   - Commission converted from lots to price units using the cross-pair point size.
4. Round the cost to the number of decimals supported by the cross pair and add one point (`PriceStep`).
5. Open arbitrage baskets when the edge exceeds the threshold:
   - **Sell synthetic / buy real cross**: Sell EURUSD, buy GBPUSD, buy EURGBP.
   - **Buy synthetic / sell real cross**: Buy EURUSD, sell GBPUSD, sell EURGBP.
6. Only one basket can be active. Before opening the opposite side, the strategy closes the relevant legs to remain flat.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `FirstLeg` | Primary leg (EURUSD) used to build the synthetic rate. Must be provided. | — |
| `SecondLeg` | Secondary leg (GBPUSD) used to build the synthetic rate. Must be provided. | — |
| `CrossPair` | Cross currency pair (EURGBP) compared to the synthetic rate. Must be provided. | — |
| `LotSizePerThousand` | Lots to trade for every 1000 units of portfolio value. Controls exposure of the basket. | `0.01` |
| `CommissionPerLot` | Total commission, in lot units, charged when trading the three legs. | `7` |
| `LogMaxDifference` | Enables diagnostic logging of the largest observed synthetic gap. | `false` |

## Position Sizing
The traded volume is calculated from the current portfolio value:
```
rawVolume = (portfolioValue / 1000) * LotSizePerThousand
volume = round_to_volume_step(rawVolume, CrossPair.VolumeStep)
volume = min(volume, CrossPair.MaxVolume)
```
The helper uses the cross pair volume step to match the broker's lot increment.

## Risk Notes
- Ensure that all three securities share the same portfolio or margin account, otherwise the basket may fail.
- The strategy assumes that market orders fill immediately. Slippage may erode the expected edge.
- Configure symbol mappings so that Level 1 data streams without interruption; stale quotes prevent basket creation.
