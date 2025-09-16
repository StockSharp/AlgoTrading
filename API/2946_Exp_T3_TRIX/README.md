# Exp T3 TRIX Strategy (ID 2946)

The Exp T3 TRIX strategy replicates the MetaTrader 5 expert advisor built around the triple-smoothed TRIX oscillator. It applies Tillson T3 smoothing to generate a fast and slow TRIX stream and reacts to momentum reversals using three selectable modes. Each mode controls how the histogram or the relative position of the fast and slow components must behave before the strategy will enter or exit a position.

## Trading Logic

- **Tillson T3 TRIX calculation**
  - Two stacks of six exponential moving averages with the same length produce Tillson T3 values for a fast and a slow stream.
  - The derivative of each T3 value (current minus previous divided by previous) becomes the TRIX histogram used for decision making.
- **Mode = Breakdown**
  - *Long entry*: Fast TRIX crosses from below zero to above zero while long entries are enabled. Any open short position is closed first (if short exits are permitted).
  - *Short entry*: Fast TRIX crosses from above zero to below zero while short entries are enabled. Any open long position is closed first (if long exits are permitted).
  - *Exit only*: When a cross occurs but the corresponding entry is disabled, the strategy still closes the opposite exposure if the relevant exit permission is enabled.
- **Mode = Twist**
  - *Long entry*: The fast TRIX slope changes from negative to positive (i.e., the current bar is rising after falling). The strategy mirrors the closing and permission rules from the Breakdown mode.
  - *Short entry*: The fast TRIX slope changes from positive to negative.
- **Mode = CloudTwist**
  - *Long entry*: The fast TRIX moves above the slow TRIX after being below it on the previous completed bar.
  - *Short entry*: The fast TRIX falls below the slow TRIX after sitting above it on the previous bar.
- **Order handling**
  - The strategy first closes the opposite exposure when a reversal signal appears and exits are allowed.
  - New orders use `Volume + |Position|` so a reversal can be executed in a single trade when permitted.
  - `StartProtection()` is activated to reuse the built-in StockSharp safety layer from the original project template.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `Fast Length` | 10 | Depth used for the fast Tillson T3 stack (six linked EMAs). |
| `Slow Length` | 18 | Depth used for the slow Tillson T3 stack. |
| `Volume Factor` | 0.7 | Tillson T3 smoothing coefficient (0 to 1). |
| `Mode` | Twist | Chooses between Breakdown, Twist, or CloudTwist signal detection. |
| `Allow Long Entry` | true | Enables opening long positions. |
| `Allow Short Entry` | true | Enables opening short positions. |
| `Allow Long Exit` | true | Enables closing long positions. |
| `Allow Short Exit` | true | Enables closing short positions. |
| `Candle Type` | 4 hour time frame | Aggregation interval used to request candles and feed the indicator chain. |

All parameters are exposed through `StrategyParam<T>` making them visible in the Designer UI and ready for optimization.

## Usage Notes

1. The logic only works with finished candles. Ensure the data source delivers the timeframe configured in `Candle Type`.
2. Because the TRIX derivative requires historical values, the first two completed candles are used for initialization and do not produce signals.
3. To replicate the MetaTrader behavior, disable the corresponding `Allow ...` flag if you want one-sided trading or exit suppression.
4. Risk management such as stop-loss or take-profit levels were not included in the original expert advisor and therefore are not implemented here. Combine the strategy with StockSharp money management modules if needed.

## Conversion Details

- Source: `MQL/2156/exp_t3_trix.mq5` plus the `t3_trix.mq5` indicator.
- API port implements the same three signal modes while using StockSharp high-level candle subscriptions and indicator classes.
- Tillson T3 smoothing is recreated using six chained exponential moving averages and the canonical 0.7 volume factor, adjustable through `Volume Factor`.
