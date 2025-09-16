# ColorJMomentum Strategy

The **ColorJMomentum Strategy** trades based on the direction changes of a Jurik smoothed momentum indicator. The approach is derived from the original MQL5 expert advisor `Exp_ColorJMomentum` and reproduced using the StockSharp high level API.

## Concept

1. Calculate the standard *Momentum* of the selected price series.
2. Smooth the momentum values with the **Jurik Moving Average (JMA)**.
3. Monitor the last two values of the smoothed momentum:
   - If the indicator was declining and turns upward, a **long** position is opened.
   - If the indicator was rising and turns downward, a **short** position is opened.
4. Position protection is handled by optional stop loss and take profit in percentage terms.

The strategy never reads historical indicator values directly. Instead it reacts only to new candle completions and stores previous values internally.

## Parameters

- **Momentum Length** – period for the momentum calculation.
- **JMA Length** – smoothing period of the Jurik moving average applied to momentum.
- **Candle Type** – timeframe used for candle subscriptions.
- **Stop Loss %** – percentage for optional stop loss.
- **Enable Stop Loss** – whether to activate stop loss.
- **Take Profit %** – percentage for take profit.
- **Enable Long** – allow opening long positions.
- **Enable Short** – allow opening short positions.

All parameters are created with `StrategyParam` so they can be optimized in Designer.

## Usage

1. Attach the strategy to the desired security.
2. Configure parameters or leave defaults (8-period momentum and 8-period JMA on 8‑hour candles).
3. Run the strategy. Orders will be issued via `BuyMarket` and `SellMarket` when momentum direction reverses.

## Notes

- The strategy processes only finished candles.
- No explicit colors are set for indicators – Designer chooses them automatically.
- The algorithm avoids any LINQ or custom collections, following project guidelines.
