# Exp HLRSign Strategy

This strategy implements the HLRSign indicator logic in StockSharp.
It opens and closes positions when the High-Low Ratio (HLR) crosses predefined levels.

## How It Works

- Calculates Donchian Channel values over a configurable range.
- Computes the HLR value as a percentage position of the mid-price within the channel.
- Generates buy or sell signals when the HLR crosses the upper or lower thresholds depending on the selected mode:
  - **ModeIn** – buy on crossing above the upper level and sell on crossing below the lower level.
  - **ModeOut** – sell on crossing below the upper level and buy on crossing above the lower level.
- Allows enabling or disabling opening and closing of long and short positions separately.

## Parameters

| Name | Description |
| --- | --- |
| `Mode` | Indicator operation mode (ModeIn or ModeOut). |
| `Range` | Lookback period for highest and lowest prices. |
| `UpLevel` | Upper threshold in percent for HLR. |
| `DnLevel` | Lower threshold in percent for HLR. |
| `CandleType` | Timeframe of candles used for calculations. |
| `BuyOpen` | Allow opening long positions. |
| `SellOpen` | Allow opening short positions. |
| `BuyClose` | Allow closing long positions. |
| `SellClose` | Allow closing short positions. |

## Notes

- The strategy uses the high-level API with `DonchianChannels` indicator.
- It processes only finished candles and checks position permissions before trading.
- No stop-loss or take-profit levels are defined; position protection can be added manually.
