# Scalping EA Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

A simple scalping system that constantly maintains two pending orders: a buy stop above the market and a sell stop below. When the market price moves too close to an order or too far away, the order is replaced to keep a fixed distance from current price. Filled orders use fixed take-profit and stop-loss offsets.

The strategy does not rely on indicators and reacts only to tick price changes.

## Details

- **Entry Criteria**:
  - Place buy stop 100 points above price and sell stop 100 points below.
  - Orders are replaced if the gap to price becomes too small or too wide.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Each order carries fixed take profit and stop loss.
- **Stops**: Yes, fixed distance.
- **Filters**: None.
