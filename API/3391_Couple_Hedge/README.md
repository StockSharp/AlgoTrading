# Couple Hedge Strategy

## Overview

The Couple Hedge strategy is a multi-currency basket trader that mirrors the
idea of the original *MT5 CoupleHedgeEA v3.0*. The strategy builds groups of two
currency pairs (``plus`` and ``minus`` sides) and trades them simultaneously to
capture relative value movements while keeping portfolio exposure hedged. It
keeps track of basket profit in account currency, opens additional hedged
positions when the basket moves into loss, and closes all trades when the profit
or loss thresholds are reached.

## Key Concepts

- **Basket trading.** Each group opens both a long and a short position to
  create a hedged basket. Additional baskets are added using configurable
  geometric or exponential progressions.
- **Profit management.** Basket profit is calculated from the per-security PnL
  published by StockSharp. Profit and loss delays (in seconds) replicate the
  tick-based wait logic of the MQL version.
- **Session guard.** Trading is paused after Monday market open and before the
  Friday close according to the configured waiting periods.
- **Spread filter.** Averaging is skipped when the average bid/ask spread of the
  watched instruments exceeds the user-defined limit.
- **Automatic sizing.** When the auto-lot option is enabled the first basket is
  sized as a percentage of the current portfolio equity. Subsequent baskets are
  scaled with the selected progression mode.

## Parameter Reference

| Parameter | Description |
|-----------|-------------|
| `OperationMode` | Overall operating mode. `CloseImmediatelyAllOrders` flattens the portfolio, while `CloseInProfitAndStop` stops after the next successful basket. |
| `SideSelection` | Choose whether the strategy trades both sides or only one side of each group. |
| `StepMode` | Governs how additional baskets are opened. `OpenWithAutoStep` measures the trigger distance from an EMA of the candle ranges. |
| `StepOpenNext` | Initial basket loss (account currency) required to open the next basket. |
| `StepProgression` & `StepProgressionFactor` | Controls how the trigger distance grows for each new basket. |
| `MinutesBetweenOrders` | Minimum waiting time between basket entries for the same group. |
| `CloseProfitMode` & `TargetCloseProfit` | Defines how baskets close in profit. `SideBySide` monitors each side independently. |
| `CloseLossMode` & `TargetCloseLoss` | Configures the forced-exit behaviour when the basket loses money. |
| `DelayCloseProfit` / `DelayCloseLoss` | Waiting period (in seconds) before executing the close once a threshold is breached. |
| `AutoLot`, `RiskFactor`, `ManualLotSize` | Volume controls. The automatic mode uses a percentage of equity divided by the latest mid price. |
| `LotProgression`, `ProgressionFactor` | Volume growth rules for subsequent baskets. |
| `UseFairLotSize` | Adjusts the lot sizes using the tick value ratio of both instruments to keep basket exposure balanced. |
| `MaximumLotSize` | Hard cap for the volume per side. Zero disables the limit. |
| `MaximumGroups` | Limits the number of simultaneously trading groups. |
| `MaximumOrders` | Limits the total number of open positions across all groups. |
| `MaxSpread` | Maximum average spread allowed before averaging is suspended. |
| `ControlSession`, `WaitAfterOpen`, `StopBeforeClose` | Session guard settings that replicate the original EA schedule. |

The individual group parameters (`GroupNEnabled`, `GroupNPlus`, `GroupNMinus`,
`GroupNCandleType`) let you configure up to three default groups. Additional
instruments can be selected directly from the UI.

## Trading Logic

1. Subscribe to candle and Level1 data for every enabled instrument.
2. Update the average candle range and mid price to feed the auto-step and
   auto-lot calculations.
3. Compute basket profit from open positions. Queue delayed exits once profit or
   loss targets are breached.
4. Respect session and risk limits (`MaximumGroups`, `MaximumOrders`, `MinutesBetweenOrders`).
5. When the basket profit is below the trigger level, open new hedged positions
   on the configured sides. Lot size is adjusted according to the progression
   rules and, optionally, the fair-lot weighting.
6. Close the entire group when profit or loss targets are confirmed after their
   respective delays.

## Usage Notes

- Ensure the selected portfolio supports trading all configured instruments and
  delivers real-time Level1 data for spread calculation.
- The strategy logs every hedge basket opening with its sequence number, which
  helps to match the behaviour with the original expert advisor.
- `MaxSpread` is measured in absolute price units because StockSharp works with
  actual bid/ask prices. For FX symbols quoted in five digits a value of `0.0004`
  corresponds to roughly four points.
- When `UseFairLotSize` is enabled, the strategy reads `Security.TickValue` to
  rebalance lots. Provide tick values in the security metadata if they are not
  supplied by the data source.
- The session filter uses the local machine time. If you need exchange-specific
  calendars adjust the values accordingly.

## Differences vs MQL Version

- The averaging trigger is expressed in account currency rather than points per
  lot. This matches the accounting information available in StockSharp.
- Tick-based delays are mapped to seconds. Use small values (1–5) to emulate the
  original behaviour.
- Visual elements of the MT5 panel (`SetChartInterface`, `SaveInformation`) are
  preserved as informational parameters only.

## Getting Started

1. Copy the strategy into your StockSharp solution and rebuild it.
2. Add the strategy to a connector, link the target portfolio, and ensure Level1
   data is available for all chosen instruments.
3. Configure the group securities and the risk parameters.
4. Start the strategy. Monitor the log for basket open/close events and adjust
   the progression settings according to your risk appetite.

