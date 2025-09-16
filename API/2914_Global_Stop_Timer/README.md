# Global Stop Timer Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Global Stop Timer Strategy is a risk-management overlay converted from the MetaTrader expert `Exp_GStop_Tm`.
It continuously watches the portfolio value on every finished candle and halts trading once a global profit target
or loss limit is reached. Additionally, it can restrict trading to a user-defined time window and force all open
positions to be flattened whenever the window is closed.

## How It Works

- When the strategy starts it records the initial portfolio balance as a reference point.
- Each time the subscribed candle series closes, the strategy reads the current portfolio value and calculates the
  difference from the initial balance.
- Depending on the selected `StopCalculationMode`, the difference is converted to a percentage or left as currency.
- If the loss exceeds `StopLoss` or the profit exceeds `TakeProfit`, the strategy enters the stopped state, logs the
  event, and sends market orders to close any remaining position.
- When the optional trading window is enabled and the current time leaves the window, the strategy also attempts to
  flatten the position. Once the position size becomes zero the stop flag is reset, allowing trading to resume inside
  the next valid window.

The strategy itself never opens new positions. It is designed to supervise other strategies or manual trades and to
protect the account from excessive drawdown or to lock in account-wide profits.

## Trading Window Logic

The trading window replicates the original expert logic:

- If the start hour is less than the end hour, trading is allowed between the start minute (inclusive) and the end
  minute (exclusive) on the same day.
- If the start and end hours are equal, trading is permitted only when the current minute is between `StartMinute`
  (inclusive) and `EndMinute` (exclusive).
- If the start hour is greater than the end hour, the session wraps past midnight. Trading is enabled from the start
  time until midnight and resumes from midnight until the end time on the following day.

## Parameters

- `StopCalculationMode` – choose between percentage-based or currency-based global stops.
- `StopLoss` – global loss threshold. Treated as a percentage when the percent mode is active, otherwise as account
  currency.
- `TakeProfit` – global profit target. Uses the same unit as `StopLoss`.
- `UseTradingWindow` – enable or disable the session filter.
- `StartHour` / `StartMinute` – starting time of the allowed trading window.
- `EndHour` / `EndMinute` – closing time of the allowed trading window.
- `CandleType` – candle series that defines how often the account state is evaluated.

## Notes

- Because stop checks happen on candle close, use a small timeframe (for example, one minute) when rapid reaction is
  required.
- The strategy closes only the position managed by this strategy instance. Run separate instances if multiple
  securities need individual supervision.
- Use alongside other trading strategies by attaching it as a parent strategy or running it on the same security to
  provide account-wide protection.
