# Personal Assistant Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This manual helper strategy reproduces the core features of the original MQL `personal_assistant` script. It monitors the current account state and exposes simple methods to place market or pending orders. The strategy does not generate trading signals; instead it acts as a utility for manual interaction and order management.

## Features

- Displays account statistics such as current PnL and open position information in the log on each candle.
- Supports market orders through `Buy()` and `Sell()` methods.
- Supports closing positions with `CloseAll()`.
- Allows changing the default trading volume via `IncreaseVolume()` and `DecreaseVolume()`.
- Optional pending orders (`BuyStop`, `SellStop`, `BuyLimit`, `SellLimit`) with configurable stop loss and take profit requirements.
- Optional legend messages describing available actions.

## Parameters

- **OrderVolume** – base volume used for manual orders.
- **AllowPending** – enable or disable placing pending orders.
- **RequireStopLoss** – require stop loss for manual orders.
- **RequireTakeProfit** – require take profit for manual orders.
- **DisplayLegend** – print the action legend on start.
- **CandleType** – type of candles for periodic updates.

## Usage

The strategy subscribes to the specified candle series and logs account information when a candle is finished. Orders can be issued by calling the exposed methods from your code or from the Designer/Shell automation tools.
