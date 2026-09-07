# RSI Streak Pending Limits Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This diagram waits for a persistent RSI extreme before placing a pullback limit order. It confirms the current RSI and its two preceding finished values, allows one pending order per excursion, cancels an order when RSI leaves its zone, and protects every fill with percentage exits.

![schema](schema.svg)

## Strategy Overview

- Finished five-minute candles feed RSI with length 14 and provide the close used to price pending orders.
- The lower zone is below 30 and the upper zone is above 70. An N-values block schedules evaluation after three finished RSI updates.
- At evaluation, Formula blocks check the current RSI and its two preceding values together with the position gate. Interrupted sequences cannot create an entry.
- A buy limit is placed 0.2% below the signal close; a sell limit is placed 0.2% above it.
- Separate Flag blocks allow only one order in each uninterrupted visit to an extreme zone.
- Filled entries receive a 1.5% take-profit and a 1% stop-loss, both submitted as market exits when activated.

## Entry and Exit Rules

- **Long entry**: After the three-update evaluation, require all three RSI values to be below 30 and Position to be less than or equal to zero. Register one buy limit at `Close × (1 − Pending Offset / 100)`.
- **Short entry**: After the three-update evaluation, require all three RSI values to be above 70 and Position to be greater than or equal to zero. Register one sell limit at `Close × (1 + Pending Offset / 100)`.
- **Pending order**: Cancel an unfilled buy limit when RSI rises above 30. Cancel an unfilled sell limit when RSI falls below 70. The same event resets that side's Flag for a later excursion.
- **Exit**: Once a pending order fills, Position protection closes its exposure at a favourable move of 1.5% or an adverse move of 1%, using a market order.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candles | 00:05:00 | Time frame of the finished candles used for signals and pricing. |
| RSI Length | 14 | Averaging length of the Relative Strength Index. |
| RSI Source | Not set | No alternate indicator input field is selected. |
| Oversold | 30 | Strict upper boundary for a three-value long setup. |
| Overbought | 70 | Strict lower boundary for a three-value short setup. |
| Match Count (N) | 3 | Number of finished RSI updates in the qualification interval. |
| Pending Offset, % | 0.2 | Distance of a limit price from the signal candle close. |
| Volume | 1 | Size of each pending entry order. |
| Take Profit, % | 1.5 | Favourable move from a filled entry that activates protection. |
| Stop Loss, % | 1 | Adverse move from a filled entry that activates protection. |
| Trailing Stop Loss | false | Keeps the stop boundary fixed. |
| Use Market Orders | true | Sends activated protective exits as market orders. |

## Diagram Details

- Current RSI feeds two Previous value blocks with shifts 1 and 2. All three values are evaluated only from finished candles.
- A shared N-values block is armed by either extreme and counts three RSI updates. Its release samples both entry-score formulas at the same evaluation point.
- The long score is negative only when all three RSI values are below 30 and Position is not positive. The short score is negative only when all three values are above 70 and Position is not negative.
- If RSI left the zone during the qualification interval, the corresponding score is not negative and no registration trigger is produced. A later extreme can start a fresh interval.
- Formula blocks calculate the two limit prices from the close, and Order registering blocks submit unrounded one-unit limit orders.
- Order cancellation keeps the latest order for each side and acts as soon as RSI crosses back through that side's boundary.
- A one-unit order against an existing one-unit opposite position first flattens that exposure; another qualified excursion is required to establish exposure in the new direction.
- The chart displays five-minute candles, RSI with both thresholds, both pending-order streams, and every strategy fill or exit.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, and adjust the RSI levels, pending offset, protection distances, and volume for the instrument before live trading.
