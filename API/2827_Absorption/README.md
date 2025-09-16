# Absorption Strategy

The strategy replicates the Absorption expert advisor for MetaTrader. It searches for "engulfing" candles that absorb the range of the previous bar and form an extreme within a short lookback. When such an absorption bar appears, the algorithm brackets the market with stop orders and manages the resulting position with a mix of fixed targets, breakeven logic, and a trailing stop.

## Trading logic

1. **Pattern detection**
   - Inspect the last two completed candles.
   - Treat a candle as an *absorption bar* when its high is above the previous candle high and its low is below the previous candle low.
   - Validate the bar by checking whether its high or low is the most extreme value within the last `MaxSearch` candles.
   - Give priority to the older candle (two bars ago). If both bars satisfy the absorption condition, the older bar is used; otherwise the most recent bar may trigger the setup.
2. **Order placement**
   - Place a buy stop order at the bar high plus the configured `Indent`.
   - Place a sell stop order at the bar low minus the same `Indent`.
   - Both orders use the common strategy volume.
   - Each pending order stores its own protective stop level and optional take-profit target. Orders automatically expire after `OrderExpirationHours` if they remain unfilled.
3. **Position management**
   - When one side is filled the opposite pending order is cancelled.
   - The initial stop is located at the opposite side of the absorption candle minus/plus the indent.
   - An optional fixed take profit closes the trade once the configured distance in price steps is reached.
   - The breakeven module moves the stop-loss to `Entry + Breakeven` (long) or `Entry - Breakeven` (short) after the price advances by `BreakevenProfit` steps.
   - The trailing stop keeps the stop-loss at `TrailingStop` distance from the best price, updating only when the price moves by at least `TrailingStep` steps in the profitable direction.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `CandleType` | Candle data type to subscribe to (default: 1-hour time frame). |
| `MaxSearch` | Number of recent candles used to confirm high/low extremes. |
| `TakeProfitBuy` | Distance in price steps for the long take-profit order. `0` disables the target. |
| `TakeProfitSell` | Distance in price steps for the short take-profit order. `0` disables the target. |
| `TrailingStop` | Trailing stop distance in price steps. `0` disables trailing. |
| `TrailingStep` | Minimum forward move required before the trailing stop is advanced. Must be positive when trailing is enabled. |
| `Indent` | Offset in price steps that is added above/below the absorption bar to define stop entry levels. |
| `OrderExpirationHours` | Lifetime of pending orders. After this period the orders are cancelled if not triggered. |
| `Breakeven` | Offset applied to the stop-loss when the breakeven rule fires. `0` disables breakeven. |
| `BreakevenProfit` | Profit threshold (in price steps) that must be reached before the stop-loss is moved to breakeven. |

All distance-based inputs are expressed as multiples of the instrument price step. The default strategy volume is set to `0.1`.

## Risk management

The strategy uses only market orders for exits. Stop-loss, take-profit, breakeven, and trailing rules monitor candle highs and lows to detect intrabar hits. Once an exit order is submitted no additional exit requests are generated until the current position is flat.
