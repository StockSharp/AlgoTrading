# Build Your Grid Strategy

The **Build Your Grid Strategy** is a direct conversion of the MetaTrader expert advisor "BuildYourGridEA". It keeps two indepen
dent ladders of market positions on the long and short side, adds new layers when price advances by a configurable number of pip
s and optionally increases the traded volume geometrically or exponentially. The basket can be closed when a combined profit targ
et is reached, when a maximum loss measured in pips is exceeded, or by issuing hedge orders whenever the floating drawdown breac
hes a percentage of the account balance.

## How it works

1. **Initial entries.** Depending on *Order Placement*, the strategy opens the first buy, sell or both market orders as soon as the spread condition allows it.
2. **Grid expansion.** Additional orders are triggered either with the trend or against it. The distance to the next layer is measured in pips, optionally multiplied by the number of already open orders or by a power of two.
3. **Volume progression.** Order size follows the selected lot progression rule (static, geometric, or exponential) and can be capped by *Max Multiplier* relative to the first entry.
4. **Profit taking.** The entire basket is closed once the aggregate floating PnL exceeds the target expressed either in pips or in account currency.
5. **Loss protection.** When the cumulative loss crosses the configured pip threshold, the strategy closes either the oldest ticket on each side or the whole basket depending on the *Loss Handling* mode.
6. **Hedging.** If the floating drawdown reaches *Hedge Threshold (%)*, a balancing order sized by the volume difference and the *Hedge Multiplier* is submitted to freeze exposure.

## Parameters

| Parameter | Description |
| --- | --- |
| `Order Placement` | Which directions are allowed for opening new layers (both, long only, short only). |
| `Grid Direction` | Whether additional orders follow the trend or fade the movement. |
| `Grid Step (pips)` | Base distance in pips to the next layer before multipliers are applied. |
| `Step Progression` | Static distance, geometric growth (× count), or exponential growth (× 2^(n-1)). |
| `Close Target` | Type of profit target (pips or account currency). |
| `Target (pips)` / `Target (currency)` | Threshold that must be exceeded to close the basket in profit. |
| `Loss Handling` | Action when the pip drawdown limit is hit (do nothing, close the first tickets, or close all). |
| `Loss (pips)` | Maximum tolerated combined loss before protection engages. |
| `Use Hedge` | Enables hedge orders to balance net exposure during deep drawdowns. |
| `Hedge Threshold (%)` | Percentage of the account balance used as a trigger for hedging. |
| `Hedge Multiplier` | Multiplier applied to the volume difference when issuing the hedge order. |
| `Auto Volume` / `Risk Factor` | Balance driven position sizing. Volume = Balance × RiskFactor / 100000. |
| `Manual Volume` | Fixed lot size when automatic sizing is disabled. |
| `Lot Progression` | Static, geometric, or exponential scaling for consecutive orders. |
| `Max Multiplier` | Caps the lot size to `firstLot × MaxMultiplier`. |
| `Max Orders` | Maximum number of simultaneous open positions (0 = unlimited). |
| `Max Spread` | Blocks new trades while the spread in pips is above the threshold (0 = ignore). |
| `Use Completed Bar` / `Candle Type` | Evaluate signals only once per completed candle of the selected type. |

## Usage notes

- The strategy relies on best bid/ask updates. Configure your data feed to supply level 1 quotations with accurate spreads.
- Hedge orders depend on the portfolio value. When running in the StockSharp Designer or Tester, ensure the connected portfolio reports a meaningful balance.
- Grid strategies accumulate risk quickly. Start with conservative volumes and test the configuration in simulation before applying it to live trading.
- When `Use Completed Bar` is enabled the trading logic is evaluated only once per finished candle, which mimics the "Use Completed Bar" option of the original advisor.
