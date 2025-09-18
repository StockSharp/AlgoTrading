# Painel Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Emulates the MetaTrader "Painel" expert advisor by publishing a dashboard of market and account metrics. The strategy subscribes to level one quotes and trade prints, then logs an aggregated status line every second.

## Overview

- **Purpose**: Informational panel that mirrors the original MQL expert.
- **Behavior**: Collects last price, session high/low, current position side, account name, current balance and cumulative profit.
- **Trading**: No orders are sent; the strategy is read-only and safe to run on live accounts.
- **Update rate**: One log entry per second through the strategy logger.

## Logged fields

1. `Symbol` – identifier of the currently selected security.
2. `Last` – latest traded price, updated from tick data.
3. `High` – session high retrieved from level one updates.
4. `Low` – session low retrieved from level one updates.
5. `Position` – textual representation of the net position (`Long`, `Short` or `Flat`).
6. `User` – account or portfolio name exposed by the connection adapter.
7. `Profit` – difference between the current portfolio value and the initial balance when the strategy started.
8. `Balance` – current portfolio value (equity) when available.

## Internal workflow

1. On start the strategy stores the initial portfolio balance and subscribes to level one quotes and trade executions.
2. Each quote update refreshes last, high and low prices; trade prints reinforce the last price when quotes do not contain it.
3. A timer fires once per second to recompute portfolio statistics and emit a formatted log message.
4. The timer stops automatically when the strategy is halted.

## Customization hints

- Attach the strategy to any timeframe or chart – it only requires access to level one data and portfolio information.
- Combine with StockSharp dashboards or custom log collectors to visualize the output similarly to the original MetaTrader panel.
- The logging interval can be adjusted by editing `Timer.Start(TimeSpan.FromSeconds(1), ...)` inside the source code if a slower or faster refresh is preferred.

## Limitations

- Historical high/low values depend on the data provider supplying `HighPrice` and `LowPrice` fields; otherwise they remain at zero.
- Portfolio statistics require a connected portfolio adapter that fills `BeginValue`, `CurrentValue` and `Name` fields.
- No on-screen graphics are produced—the information is available through the log stream only.
