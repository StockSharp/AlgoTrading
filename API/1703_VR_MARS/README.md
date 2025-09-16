# VR MARS Strategy

This sample demonstrates a simplified port of the manual trading panel **VR---MARS-EN** from MQL4 to StockSharp.

The original script provided five predefined lot sizes and buttons to send buy or sell orders. It also displayed multiple labels with trading statistics. In this C# version the visual panel is removed while the core idea of selecting one of the five lot sizes and executing a market order is preserved.

## Parameters

- `Lot1` – size of the first lot.
- `Lot2` – size of the second lot.
- `Lot3` – size of the third lot.
- `Lot4` – size of the fourth lot.
- `Lot5` – size of the fifth lot.
- `SelectedLot` – number from 1 to 5 specifying which lot size will be used.
- `Buy` – when `true` a market buy order is sent on strategy start.
- `Sell` – when `true` a market sell order is sent on strategy start.

Only one of the direction flags should be enabled at a time. When the strategy starts it activates position protection and sends the corresponding market order using high level helper methods.

## Notes

This strategy is intended for educational purposes and does not implement any trading logic beyond immediate order execution.
