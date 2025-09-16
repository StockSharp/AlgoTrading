# ColorMETRO Strategy

This strategy trades based on the ColorMETRO indicator, which builds fast and slow step lines around the RSI.
A long position is opened when the fast line crosses above the slow line. A short position is opened when the fast line crosses below the slow line. Opposite positions are closed on the same signals.

## Parameters
- **Candle Type** – candle type used for calculations.
- **RSI Period** – period for RSI calculation.
- **Fast Step** – step size for the fast line.
- **Slow Step** – step size for the slow line.
- **Stop Loss** – distance in points for stop-loss protection.
- **Take Profit** – distance in points for take-profit protection.
- **Allow Buy** – permission to open long positions.
- **Allow Sell** – permission to open short positions.
- **Close Long** – permission to close long positions.
- **Close Short** – permission to close short positions.

The strategy uses `StartProtection` to manage stop-loss and take-profit levels.
