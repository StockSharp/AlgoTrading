import clr
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Messages")

from System import TimeSpan, Array, Math
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import CandleStates
from datatype_extensions import *

class grid_bot_strategy(Strategy):
    """Grid trading bot.

    Divides a price range into levels and buys near support while selling near
    resistance. Designed for sideways markets where price oscillates within a
    known corridor.
    """

    def __init__(self):
        super(grid_bot_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._upper = self.Param("UpperLimit", 48000.0)
        self._lower = self.Param("LowerLimit", 45000.0)
        self._grid_count = self.Param("GridCount", 10)

        self._grid = []

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(grid_bot_strategy, self).OnReseted()
        self._grid = []

    def OnStarted(self, time):
        super(grid_bot_strategy, self).OnStarted(time)
        step = (self._upper.Value - self._lower.Value) / self._grid_count.Value
        self._grid = [self._lower.Value + step * i for i in range(self._grid_count.Value + 1)]
        sub = self.SubscribeCandles(self.candle_type)
        sub.Bind(self._on_process).Start()

    def _nearest_level(self, price):
        return min(self._grid, key=lambda x: abs(x - price))

    def _on_process(self, candle):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        price = candle.ClosePrice
        level = self._nearest_level(price)
        mid = (self._upper.Value + self._lower.Value) / 2
        if price <= level and price < mid and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif price >= level and price > mid and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))

    def CreateClone(self):
        return grid_bot_strategy()
