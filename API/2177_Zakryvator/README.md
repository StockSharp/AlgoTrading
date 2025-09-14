# Zakryvator Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Zakryvator strategy is a risk management module that monitors the current open position and closes it when the unrealized loss exceeds a predefined threshold. The allowed loss depends on the position volume, replicating the logic of the original MQL script where different lot sizes correspond to different maximum drawdowns.

This strategy does not generate entries by itself. Positions are expected to be opened manually or by another strategy. Zakryvator simply protects the account by exiting losing trades automatically.

## Details

- **Entry Criteria**: None. The strategy only manages existing positions.
- **Exit Criteria**: Closes the current position once the loss reaches the configured threshold for its volume.
- **Long/Short**: Both directions are supported.
- **Stops**: Uses fixed monetary loss limits that vary with the position size.
- **Filters**: No additional filters.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `Min001002` | Maximum loss for positions with volume ≤ 0.02 lots. |
| `Min002005` | Maximum loss for positions with volume between 0.02 and 0.05 lots. |
| `Min00501` | Maximum loss for positions with volume between 0.05 and 0.10 lots. |
| `Min0103` | Maximum loss for positions with volume between 0.10 and 0.30 lots. |
| `Min0305` | Maximum loss for positions with volume between 0.30 and 0.50 lots. |
| `Min051` | Maximum loss for positions with volume between 0.50 and 1 lot. |
| `MinFrom1` | Maximum loss for positions with volume greater than 1 lot. |

## Behavior

1. The strategy subscribes to trade ticks to track real-time prices.
2. On each tick it calculates the unrealized PnL using the current price and the average entry price.
3. If the loss exceeds the threshold corresponding to the current position volume, the position is closed at market.

This makes Zakryvator a simple but effective tool for limiting drawdowns based on trade size.

