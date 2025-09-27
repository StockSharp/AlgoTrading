# Simple DCA Strategy

This strategy places a base order and adds safety orders when price deviates by a specified percentage. It exits once price reaches a take profit calculated from the average entry price. Each safety order size is multiplied by a factor.

## Parameters
- Candle Type
- Base order size (quote currency)
- Price deviation for safety order (%)
- Maximum safety orders
- Take profit (%)
- Order size multiplier
