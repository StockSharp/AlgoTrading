# Simple Hedge Panel Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Simple Hedge Panel Strategy recreates the behaviour of the original MetaTrader 5 "Simple Hedge Panel" expert advisor inside StockSharp. The original panel allowed a trader to configure up to five deal boxes, each with its own symbol, order direction, and volume, and then manually open or close every configured position with a single button press. This conversion keeps the manual workflow: starting the strategy submits all configured market orders at once, and stopping the strategy (or calling the close method explicitly) flattens every tracked position.

Unlike automated signal strategies, this component is meant for semi-manual hedging. It does not analyse indicators or candles. Instead, it acts as an execution helper that can quickly stage simultaneous entries across multiple instruments with predefined sizes and sides. This is especially useful when hedging correlated assets, splitting exposure across brokers, or manually balancing a portfolio between bullish and bearish bets.

## How it works

1. Configure the number of active slots (1 to 5) with the `Slots` parameter.
2. For each slot provide:
   - The security to trade (`Slot N Security`).
   - The order volume in lots (`Slot N Volume`).
   - The desired side (`Slot N Buy` set to `true` for a buy and `false` for a sell).
3. Assign a portfolio and, optionally, the default strategy security that will be used when a slot has no explicit security.
4. Start the strategy to send market orders for every valid slot. All trades share the same start timestamp just like the "Open Hedge Positions" button in the MT5 panel.
5. Stop the strategy or call `CloseConfiguredPositions()` to flatten positions that were opened through the slots. The method can also be triggered while the strategy is running to manually perform the "Close Hedge Positions" action.

## Parameters

- **Slots** – number of slots to process. Values outside 1-5 are automatically clamped with a log message.
- **Slot N Security** – security assigned to the slot. If left empty the strategy falls back to the main `Security` property.
- **Slot N Volume** – order volume for the slot. Must be greater than zero to submit an order.
- **Slot N Buy** – when `true` the slot submits a buy market order, otherwise it submits a sell market order.

Every slot is independent, so a mixture of long and short entries can be opened across different instruments. Any slot with missing data is skipped with an informative log entry so that the operator knows why an order was not transmitted.

## Trading logic

- Orders are only submitted when the strategy has an assigned portfolio. If the portfolio is missing, an error is logged and no trades are sent.
- Slot validation happens on every open or close request. Invalid slot count, empty securities, or non-positive volumes do not block other valid slots.
- The strategy stores a flag indicating whether hedge orders were issued. This prevents repeated submissions unless the positions are closed first, mirroring the manual workflow of the original panel.
- `CloseConfiguredPositions()` looks up the current portfolio position for each configured security and sends a market close order when exposure is non-zero. It can be called manually at any time, not just during shutdown.

## Notes

- The strategy does not rely on candles or indicators and therefore does not create subscriptions automatically. It simply executes the configured market orders.
- You can mix unique securities and the default strategy security across slots, which allows the panel to open several orders on the same instrument if desired.
- Use the StockSharp log to monitor skipped slots, submitted orders, and closing events. All diagnostic messages are written in English to match the coding guidelines.
- Because everything is driven by manual configuration, optimisation is disabled for the slot parameters; they are designed for discretionary trading sessions rather than backtests.
