# BITEX.ONE Market Maker Strategy

## Overview
The **BitexOneMarketMakerStrategy** is a conversion of the MetaTrader 5 expert advisor "BITEX.ONE MarketMaker". The strategy maintains symmetric ladders of limit orders around a reference (lead) price and balances the resulting inventory so that the net position stays close to a configurable target. It is designed for high-frequency market-making on spot or derivatives instruments where rebates and spread capture are the main revenue source.

## Trading Logic
1. Determine the lead price using the selected order-book side (bid, ask, or midpoint) from either the traded security or an optional lead security.
2. Compute price offsets for every quoting level using the horizontal `PriceShift` coefficient and the optional `VerticalShift` bias.
3. Calculate how much inventory should be distributed per level based on the `MaxVolumePerLevel`, the current `Position`, and the `DesiredPosition` target.
4. Place or update buy and sell limit orders for each level, respecting the security tick size and volume constraints.
5. Continuously monitor order book updates and order state changes to keep the quotes synchronized with the lead price. Orders are cancelled and reissued if price or volume drifts beyond half of a tick/step.
6. If the strategy lacks sufficient volume to satisfy the minimum lot size for another level, the leftover exposure is ignored until more inventory becomes available.

## Parameters
- **MaxVolumePerLevel** – Maximum volume (lots) allowed at each quoting level. Also defines the total available inventory window for distribution on either side.
- **PriceShift** – Relative coefficient applied to the lead price to obtain the horizontal distance between levels. Example: `0.001` corresponds to 0.1% of the lead price.
- **VerticalShift** – Absolute coefficient multiplied by the lead price and applied equally to buy and sell quotes, effectively biasing the quoting centre.
- **LevelCount** – Number of quoting levels per side. Levels are indexed from the inside out; higher indices are placed further from the lead price.
- **DesiredPosition** – Inventory target. Positive values bias exposure to the long side; negative values encourage a net short inventory.
- **LeadSecurityId** – Optional identifier of another security that should supply the lead price (for example, an index or mark instrument). Leave empty to use the traded security.
- **LeadPriceSide** – Side of the order book used as the lead price: `Bid`, `Ask`, or `Midpoint`.

## Data Subscriptions
The strategy subscribes to the market depth (order book) of the traded security. When `LeadSecurityId` is provided, it also subscribes to the order book of the referenced security to retrieve the lead price. Both subscriptions are required for the quoting logic to operate.

## Order Management
- Limit orders are registered with `BuyLimit` and `SellLimit` helpers.
- Orders that move away from the required price or volume tolerance are cancelled and reissued on the next update cycle.
- Finished, cancelled, or rejected orders are removed from the managed slots immediately after receiving an order-change notification.

## Usage Notes
- Ensure that the selected securities expose level 2 data and accurate tick/lot steps.
- Start the strategy only after the connection is established; otherwise the required order book streams will not be available.
- Adjust `MaxVolumePerLevel` and `LevelCount` carefully to respect the exchange risk limits and inventory constraints.
- The strategy starts the platform protection module once at launch; add custom stops or hedges as required for your venue.

