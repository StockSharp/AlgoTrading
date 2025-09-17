# Risk Profit Closer Strategy

## Overview
The **Risk Profit Closer Strategy** continuously supervises the currently traded symbol and forces an exit once the floating profit or loss of the open position reaches user-defined percentages of the account equity. The original MetaTrader script polled every tick, measured the distance between the entry price and the latest bid/ask quote, and closed positions whose unrealised profit exceeded the configured limits. The StockSharp port keeps the exact behaviour by watching Level1 quotes and triggering the same percentage-based checks.

Unlike a typical entry strategy, this module never opens new positions. It is intended to run alongside other strategies or manual trades as a risk guard that liquidates positions when the floating result violates the configured limits.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `RiskPercentage` | Maximum loss tolerated per position, expressed as a percentage of the account equity. | `1` |
| `ProfitPercentage` | Target profit that forces a position to be closed, expressed as a percentage of the account equity. | `2` |
| `TimerInterval` | Interval used to re-check the portfolio when no fresh quotes arrive. | `00:00:01` |

## Trading Logic
1. When the strategy starts it validates that both `Security` and `Portfolio` are assigned, subscribes to `SubscribeLevel1()` to receive bid/ask updates, and schedules a periodic timer using `TimerInterval`.
2. Every new Level1 change or timer tick invokes the risk evaluation routine. The routine obtains the current equity (`Portfolio.CurrentValue`, falling back to `Portfolio.BeginValue`) and converts the configured percentages into absolute money amounts.
3. For the active symbol position the strategy calculates the floating profit by comparing the entry price with the relevant side of the market (`BestBidPrice` for long positions, `BestAskPrice` for shorts). Price differences are converted into monetary values using the security's price step and step price when available.
4. If the floating profit is greater than or equal to `ProfitPercentage × equity`, or the floating loss is greater than or equal to `RiskPercentage × equity`, the position is closed immediately via `ClosePosition`.

## Conversion Notes
- The MetaTrader function `CheckTrades()` iterated over all positions and compared the symbol name with `Symbol()`. The StockSharp version filters `Portfolio.Positions` by the configured `Security` reference to replicate the same scope.
- MetaTrader used `symbolInfo.Ask()` for buy positions and `symbolInfo.Bid()` for sell positions. The port preserves this asymmetry by preferring the most recent `BestAskPrice`/`BestBidPrice` quotes and falling back to last trade/last price snapshots when quotes are unavailable.
- Profit calculations in MetaTrader multiplied the price difference by `SymbolInfo.Point()`. StockSharp exposes `PriceStep` and `StepPrice`, so the port converts the price delta into money using those values when present and falls back to a raw price × volume product otherwise.
- The timer emulates the `OnTick` polling loop to ensure that the protection still fires even when the market is idle or the data feed delivers sparse updates.

## Usage Notes
- Assign both `Security` and `Portfolio` before starting the strategy. Without portfolio valuation (`Portfolio.CurrentValue` or `Portfolio.BeginValue`) the risk thresholds cannot be computed.
- The strategy assumes that positions are reported in netted form (a single long or short quantity per instrument). If multiple hedged positions exist, close-out commands will flatten the net exposure exactly like the original script.
- Running this strategy alongside another trading strategy is safe: it does not create new orders unless the risk or profit thresholds are hit.
