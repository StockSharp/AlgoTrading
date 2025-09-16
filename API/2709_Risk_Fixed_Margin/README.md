# Risk Fixed Margin Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy is a StockSharp port of the MetaTrader 5 script `risk (barabashkakvn's edition).mq5`. The original expert adviser does not trade – it calculates how many lots may be opened when a fixed percentage of the free margin is risked. The StockSharp version keeps the same goal: it watches Level 1 quotes, evaluates the available capital, and logs detailed recommendations for buy and sell orders.

## Overview

- **Purpose:** monitor free margin consumption and translate it into position size suggestions for both directions.
- **Data subscription:** Level 1 (best bid and ask) of the strategy's security.
- **Trading activity:** the strategy never sends orders; it only produces informational log records.
- **Output:** periodic multi-line messages with the computed lot sizes and current account metrics.

## Parameter

| Name | Description | Default |
| ---- | ----------- | ------- |
| `Risk %` | Percentage of the estimated free margin that may be allocated to a new position. | `5` |

## Runtime Workflow

1. When the strategy starts it subscribes to Level 1 data to receive bid/ask updates. No candle or indicator subscriptions are required.
2. Each Level 1 update refreshes the stored best bid and best ask prices. As soon as at least one side is known the strategy builds a status report.
3. The report approximates the account metrics:
   - **Equity** uses `Portfolio.CurrentValue` (current total account value).
   - **Balance** is estimated as `equity - PnL`, which removes the strategy PnL so the number resembles the MetaTrader balance without floating profit.
   - **Free margin** subtracts the capital currently tied up in the open position (`abs(Position) * mid price`) from the equity. The mid price is the average of bid and ask when both are available, or the known side otherwise.
4. The available free margin is multiplied by the `Risk %` parameter to obtain the monetary risk budget.
5. The script computes a raw volume for both sides: `risk budget / price`. The raw volume is then normalized down to the closest multiple of `Security.VolumeStep` to respect broker lot rules.
6. A five-line log entry is produced. The content mirrors the original MetaTrader `Comment` text: the first line states the risk percentage, the next two lines describe the long side, and the final two lines describe the short side. Messages are only emitted when the values change to avoid log flooding.

## Output Example

```
5% risk of free margin
Check open BUY: 0.42, Balance: 10000.00, Equity: 10050.00, FreeMargin: 9950.00
trade BUY, volume: 0.40
Check open SELL: 0.41, Balance: 10000.00, Equity: 10050.00, FreeMargin: 9950.00
trade SELL, volume: 0.40
```

The example shows how the strategy reports both the theoretical maximum volume (`Check open ...`) and the normalized order volume after rounding to the volume step (`trade ...`).

## Differences from the MetaTrader Script

- StockSharp portfolios expose different fields than MetaTrader accounts. Balance and free margin are approximated using `Portfolio.CurrentValue`, realized PnL, and the notional value of the strategy position.
- The money management helper `CMoneyFixedMargin` is replaced with explicit calculations. Risk is still defined as a percentage of free funds, but the computation is transparent and easy to adjust.
- StockSharp does not have a direct equivalent of `CTrade.CheckVolume`, so the normalized volume is limited to the closest lower multiple of `VolumeStep`. Additional broker-specific constraints (maximum lot, margin tiers, etc.) can be added where necessary.
- Instead of updating the chart comment, the strategy writes to the log through `LogInfo`. In Designer or Runner the text will appear in the strategy log window.

## Usage Tips

1. Attach the strategy to a portfolio and security whose tick size and volume step reflect the intended market.
2. Adjust `Risk %` to match the desired risk tolerance. Because the risk budget is recalculated on each quote, using large percentages with high leverage products can lead to very large theoretical volumes.
3. If you require other capital constraints (for example, a hard cap on order size or on margin usage), extend `CalculateVolumes` or `NormalizeVolume` with the additional rules.
4. The script is informational and does not send orders; combine it with execution strategies or manual trading to act on the recommendations.
