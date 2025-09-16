[Русский](README_ru.md) | [中文](README_cn.md)

Sea Dragon 2 is a hedging grid strategy that opens positions in both directions and adds new orders when price moves by a user defined step. Order sizes follow a predefined sequence and take profit levels adapt depending on the balance between long and short exposure.

## Details

- **Initial Orders**: Opens both a buy and a sell order with the same volume at start.
- **Order Addition**: When the market moves by *Step* points from the last order price, a new pair of orders is added. The side with more exposure receives the larger order according to the sequence.
- **Volume Sequence**: 1,1,2,3,6,9,14,22,33,48,82,111,122,164,185 scaled by *Volume Scale*.
- **Take Profit**:
  - When long and short counts are equal, each side uses *Take Profit*.
  - If one side dominates, that side uses *Alt Take Profit* while the other keeps *Take Profit*.
- **Stop Loss**: Each side has a stop placed *Max Stop* points away from its average price.
- **Data Source**: Strategy operates on completed candles of type *Candle Type*.
- **Long/Short**: Both, hedged.
- **Exit**: Positions close when price hits take profit or stop levels.
