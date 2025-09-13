# OCO Order Execution Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy replicates an "One Cancels the Other" order ticket originally written for MetaTrader. It allows the trader to define up to four independent price triggers:

- **Buy Limit Price**
- **Sell Limit Price**
- **Buy Stop Price**
- **Sell Stop Price**

The strategy subscribes to Level1 data to continuously monitor the best bid and ask. When a trigger price is reached, it submits a market order in the corresponding direction. After an order is executed, stop-loss and take-profit protections are applied using distances measured in pips. These distances are automatically converted to absolute prices based on the security's `PriceStep`.

When the **OCO mode** is enabled, hitting any trigger will automatically disable all other triggers, effectively implementing the classic one-cancels-the-other behavior. If OCO mode is disabled, other triggers remain active and can open additional positions as prices continue to move.

## Details

- **Entry Criteria**:
  - Long when `Ask <= BuyLimitPrice` (Buy Limit trigger).
  - Long when `Ask >= BuyStopPrice` (Buy Stop trigger).
  - Short when `Bid >= SellLimitPrice` (Sell Limit trigger).
  - Short when `Bid <= SellStopPrice` (Sell Stop trigger).
- **Long/Short**: Both.
- **Exit Criteria**:
  - Positions are closed automatically by predefined stop-loss or take-profit levels.
- **Stops**: Yes, stop-loss and take-profit in pips.
- **Default Values**:
  - `StopLossPips` = 300.
  - `TakeProfitPips` = 300.
  - `OCO Mode` = enabled.
- **Filters**:
  - Category: Order execution.
  - Direction: Both.
  - Indicators: None.
  - Stops: Yes.
  - Complexity: Simple.
  - Timeframe: Tick-based.
  - Seasonality: No.
  - Neural networks: No.
  - Divergence: No.
  - Risk level: Medium.
