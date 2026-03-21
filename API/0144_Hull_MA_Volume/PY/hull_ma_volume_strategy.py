import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import HullMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class hull_ma_volume_strategy(Strategy):
    """
    Strategy that uses Hull Moving Average for trend direction.
    Enters when HMA direction changes.
    """

    def __init__(self):
        super(hull_ma_volume_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._hull_period = self.Param("HullPeriod", 9) \
            .SetRange(5, 30) \
            .SetDisplay("Hull MA Period", "Period of the Hull Moving Average", "Indicators")

        self._cooldown_bars = self.Param("CooldownBars", 100) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "General") \
            .SetRange(5, 500)

        self._prev_hull_value = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    @property
    def hull_period(self):
        return self._hull_period.Value

    @hull_period.setter
    def hull_period(self, value):
        self._hull_period.Value = value

    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    @cooldown_bars.setter
    def cooldown_bars(self, value):
        self._cooldown_bars.Value = value

    def OnStarted(self, time):
        super(hull_ma_volume_strategy, self).OnStarted(time)

        self._prev_hull_value = 0.0
        self._cooldown = 0

        hull_ma = HullMovingAverage()
        hull_ma.Length = self.hull_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(hull_ma, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, hull_ma)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, hull_value):
        if candle.State != CandleStates.Finished:
            return

        hv = float(hull_value)

        if self._prev_hull_value == 0:
            self._prev_hull_value = hv
            return

        rising = hv > self._prev_hull_value
        falling = hv < self._prev_hull_value
        self._prev_hull_value = hv

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        # Entry: HMA turning up
        if rising and self.Position == 0:
            self.BuyMarket()
            self._cooldown = self.cooldown_bars
        # Entry: HMA turning down
        elif falling and self.Position == 0:
            self.SellMarket()
            self._cooldown = self.cooldown_bars

        # Exit long: HMA turns down
        if self.Position > 0 and falling:
            self.SellMarket()
            self._cooldown = self.cooldown_bars
        # Exit short: HMA turns up
        elif self.Position < 0 and rising:
            self.BuyMarket()
            self._cooldown = self.cooldown_bars

    def OnReseted(self):
        super(hull_ma_volume_strategy, self).OnReseted()
        self._prev_hull_value = 0.0
        self._cooldown = 0

    def CreateClone(self):
        return hull_ma_volume_strategy()
