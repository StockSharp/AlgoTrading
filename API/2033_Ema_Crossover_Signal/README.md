# EMA Crossover Signal Strategy

This strategy trades the crossover of two Exponential Moving Averages (EMA). A faster EMA and a slower EMA are calculated from the chosen candle series. When the fast EMA crosses above the slow EMA the strategy can close any existing short position and optionally open a long position. When the fast EMA crosses below the slow EMA it can close a long position and optionally open a short position.

To manage risk, the strategy allows placing take profit and stop loss orders after a new position is opened. Both distances are specified in ticks. These protective orders are cancelled and recreated on each new entry.

The strategy provides separate switches for enabling or disabling long and short entries as well as for independently closing long and short positions on the opposite signal. All calculations use only finished candles.

## Parameters
- **Fast Period** – length of the fast EMA.
- **Slow Period** – length of the slow EMA.
- **Candle Type** – timeframe of candles used for calculations.
- **Allow Buy Open** – open long when the fast EMA crosses above the slow EMA.
- **Allow Sell Open** – open short when the fast EMA crosses below the slow EMA.
- **Allow Buy Close** – close long when the fast EMA crosses below the slow EMA.
- **Allow Sell Close** – close short when the fast EMA crosses above the slow EMA.
- **Take Profit Ticks** – take profit distance in ticks from the entry price.
- **Stop Loss Ticks** – stop loss distance in ticks from the entry price.
