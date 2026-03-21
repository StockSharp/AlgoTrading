import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class any_range_cloud_tail_system_tm_plus_strategy(Strategy):
    """
    AnyRange Cloud Tail System TM Plus strategy.
    Uses Highest/Lowest channel midline crossover for entries.
    """

    def __init__(self):
        super(any_range_cloud_tail_system_tm_plus_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(60)) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._period = self.Param("Period", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Period", "Channel lookback period", "Indicators")

        self._prev_mid = None

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, v): self._candle_type.Value = v
    @property
    def Period(self): return self._period.Value
    @Period.setter
    def Period(self, v): self._period.Value = v

    def OnReseted(self):
        super(any_range_cloud_tail_system_tm_plus_strategy, self).OnReseted()
        self._prev_mid = None

    def OnStarted(self, time):
        super(any_range_cloud_tail_system_tm_plus_strategy, self).OnStarted(time)

        self._prev_mid = None

        highest = Highest()
        highest.Length = self.Period
        lowest = Lowest()
        lowest.Length = self.Period

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(highest, lowest, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, highest)
            self.DrawIndicator(area, lowest)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, high, low):
        if candle.State != CandleStates.Finished:
            return

        mid = (high + low) / 2.0
        close = float(candle.ClosePrice)
        open_price = float(candle.OpenPrice)

        if self._prev_mid is None:
            self._prev_mid = mid
            return

        if close > mid and open_price <= self._prev_mid and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif close < mid and open_price >= self._prev_mid and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_mid = mid

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return any_range_cloud_tail_system_tm_plus_strategy()
