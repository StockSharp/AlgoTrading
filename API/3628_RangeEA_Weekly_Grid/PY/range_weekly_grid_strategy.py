import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class range_weekly_grid_strategy(Strategy):
    def __init__(self):
        super(range_weekly_grid_strategy, self).__init__()
        self._range_period = self.Param("RangePeriod", 100).SetDisplay("Range Period", "Candles to determine range", "Logic")
        self._grid_levels = self.Param("GridLevels", 5).SetDisplay("Grid Levels", "Grid levels within range", "Logic")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Primary candle type", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnStarted(self, time):
        super(range_weekly_grid_strategy, self).OnStarted(time)

        highest = Highest()
        highest.Length = self._range_period.Value
        lowest = Lowest()
        lowest.Length = self._range_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(highest, lowest, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, highest_val, lowest_val):
        if candle.State != CandleStates.Finished:
            return
        if highest_val <= 0 or lowest_val <= 0 or highest_val <= lowest_val:
            return

        rng = highest_val - lowest_val
        if rng <= 0:
            return

        grid_step = rng / (self._grid_levels.Value + 1)
        close = candle.ClosePrice
        mid = (highest_val + lowest_val) / 2.0

        if close <= lowest_val + grid_step and self.Position <= 0:
            self.BuyMarket()
        elif close >= highest_val - grid_step and self.Position >= 0:
            self.SellMarket()
        elif self.Position > 0 and close >= mid:
            self.SellMarket()
        elif self.Position < 0 and close <= mid:
            self.BuyMarket()

    def CreateClone(self):
        return range_weekly_grid_strategy()
