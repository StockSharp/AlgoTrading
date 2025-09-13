# Pure Martingale Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy implements a basic martingale system. It opens trades in a random direction and doubles the position size and the stop/take distance after each losing trade. After a winning trade it resets to the initial volume and distance.

The approach assumes that price will eventually return to profitability, but risk grows exponentially. Use only on liquid instruments with tight spreads.

## Details

- **Entry Criteria**:
  - No open position: randomly buy or sell on candle close.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Close when price moves in favor or against the position by the configured distance.
- **Stops**: Virtual stop loss and take profit managed by the strategy.
- **Filters**:
  - None.
