# Drag SL/TP Manager Strategy

This strategy automatically places stop-loss and take-profit orders at a fixed distance from the executed trade price. It is useful when manual positions should be protected immediately after entry.

## Parameters

- **Auto Set SL** (`bool`): enable automatic stop-loss placement.
- **SL Points** (`decimal`): stop-loss distance in price steps.
- **Auto Set TP** (`bool`): enable automatic take-profit placement.
- **TP Points** (`decimal`): take-profit distance in price steps.

## Behavior

When the strategy starts it calls `StartProtection` with the selected distances. Any position opened while the strategy is running will immediately receive the corresponding protective orders. The distances are measured in price steps (`Security.PriceStep`).

The strategy itself does not generate trade signals; it simply manages protective orders for positions opened manually or by other strategies.

## Notes

- Designed for high-level API usage.
- Only the finished candle state should trigger trading actions in extended versions.
- No graphical dragging feature from the original MQL script is implemented.
