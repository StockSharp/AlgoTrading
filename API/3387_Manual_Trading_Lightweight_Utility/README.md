# Manual Trading Lightweight Utility Strategy

## Overview
The **Manual Trading Lightweight Utility Strategy** replicates the behaviour of the MT4 "Manual Trading Lightweight Utility" panel using the StockSharp high level strategy API. It exposes the same interactive controls as strategy parameters so that the operator can toggle between market, limit and stop orders, adjust automatic price calculation, configure volume management and attach risk controls without relying on custom chart objects.

The strategy is designed for discretionary trading. Orders are triggered manually by changing the `Send Buy Order` or `Send Sell Order` parameters in the UI. Every command is acknowledged immediately, while the strategy keeps all calculations — such as automatic price suggestions and risk levels — synchronised with real time market data.

## Key Features
- **Manual order dispatch** for both buy and sell sides with support for market, limit and stop orders.
- **Automatic price suggestion** that mirrors the MT4 panel logic, updating the proposed limit or stop price from the latest bid/ask stream.
- **Optional manual price mode** that lets the operator type the desired trigger level while respecting instrument step sizes.
- **Volume management** with a global lot size and individual buy/sell volumes when the lot control switch is enabled.
- **Integrated stop-loss and take-profit management** implemented in the strategy layer to emulate order-attached protections on MT4.
- **Detailed feedback** through parameters that always reflect the latest computed entry levels for both sides.

## Conversion Notes
- The MT4 chart objects (buttons, labels and edit boxes) are replaced by strategy parameters grouped in logical sections for easy access in Hydra/Terminal.
- Protective stops and targets are handled internally by observing the live market price because StockSharp does not embed them into pending orders the same way as MT4.
- Price offsets expressed in points reuse the instrument metadata (`PriceStep` and `VolumeStep`) so that limits and stops always respect exchange constraints.

## Usage
1. Attach the strategy to a security and portfolio in Hydra or Terminal.
2. Configure the default lot size, risk parameters and price offsets.
3. Optionally enable `Lot Control` to maintain independent volumes for the buy and sell buttons.
4. Pick the order type (market, pending limit or pending stop) and whether the trigger price should follow the market or remain manual.
5. When ready, toggle `Send Buy Order` or `Send Sell Order` to `true`. The strategy will submit the corresponding order and reset the flag to `false` once processed.
6. The protection manager will close open positions at the configured stop-loss or take-profit levels calculated from the executed entry price.

## Files
- `CS/ManualTradingLightweightUtilityStrategy.cs` – C# implementation of the strategy.
- `README.md` – English documentation (this file).
- `README_cn.md` – Simplified Chinese documentation.
- `README_ru.md` – Russian documentation.

