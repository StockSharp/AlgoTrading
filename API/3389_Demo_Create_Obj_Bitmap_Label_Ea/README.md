# Demo Create OBJ BITMAP LABEL EA Strategy (StockSharp)

## Overview
This strategy ports the MetaTrader 5 example **Demo_Create_OBJ_BITMAP_LABEL_EA.mq5** (ID 289) into the StockSharp high level API. The original expert demonstrates how to place a bitmap label button on the chart. The StockSharp version mimics this behavior by drawing an alternating label that switches between two customizable text values, representing the pressed and released states of a virtual button.

The implementation subscribes to candles, renders them on a chart area, and periodically updates the label text to emulate button state transitions. Detailed parameters allow you to change the candle source, displayed texts, vertical offset, and switching frequency.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `CandleType` | Candle type used for subscription and chart rendering. | 1-minute time frame |
| `PressedText` | Text shown when the virtual button is considered pressed. | `€` |
| `ReleasedText` | Text shown when the virtual button is considered released. | `$` |
| `PriceOffset` | Vertical offset added to the candle close for placing the label. | `0` |
| `SwitchInterval` | Number of finished candles between label toggles. | `1` |

## How It Works
1. The strategy subscribes to the configured candle stream and creates a chart area.
2. Once the chart area is ready, the candle series is drawn so the label can be anchored visually.
3. Every time a candle closes, a counter increases. When the counter reaches the chosen `SwitchInterval`, the label text toggles between the pressed and released values and is drawn near the candle close plus the offset.
4. The label continues alternating as more candles arrive, simulating the image state changes of the original MQL button.

## Usage Notes
- Set `SwitchInterval` to control how frequently the label changes state. Use `1` to flip on every closed candle or higher numbers for slower updates.
- Adjust `PriceOffset` if the label overlaps candles or other drawings on the chart.
- The strategy focuses purely on visualization and does not place any orders.
- The text values can be any Unicode characters, making it easy to imitate iconography such as currency symbols or emoji.
