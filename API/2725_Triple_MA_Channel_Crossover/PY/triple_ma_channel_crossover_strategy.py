import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SmoothedMovingAverage, Highest, Lowest
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class triple_ma_channel_crossover_strategy(Strategy):
    """Triple smoothed MA crossover with Donchian-style channel for SL/TP."""
    def __init__(self):
        super(triple_ma_channel_crossover_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 5).SetGreaterThanZero().SetDisplay("Fast MA", "Fast period", "MAs")
        self._mid_period = self.Param("MiddlePeriod", 15).SetGreaterThanZero().SetDisplay("Middle MA", "Middle period", "MAs")
        self._slow_period = self.Param("SlowPeriod", 30).SetGreaterThanZero().SetDisplay("Slow MA", "Slow period", "MAs")
        self._channel_period = self.Param("ChannelPeriod", 15).SetGreaterThanZero().SetDisplay("Channel", "Highest/Lowest period", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4).TimeFrame()).SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(triple_ma_channel_crossover_strategy, self).OnReseted()
        self._prev_fast = 0
        self._prev_mid = 0
        self._prev_slow = 0
        self._has_prev = False
        self._entry_price = 0
        self._stop_price = 0
        self._take_price = 0

    def OnStarted(self, time):
        super(triple_ma_channel_crossover_strategy, self).OnStarted(time)
        self._prev_fast = 0
        self._prev_mid = 0
        self._prev_slow = 0
        self._has_prev = False
        self._entry_price = 0
        self._stop_price = 0
        self._take_price = 0

        fast = SmoothedMovingAverage()
        fast.Length = self._fast_period.Value
        mid = SmoothedMovingAverage()
        mid.Length = self._mid_period.Value
        slow = SmoothedMovingAverage()
        slow.Length = self._slow_period.Value
        hi = Highest()
        hi.Length = self._channel_period.Value
        lo = Lowest()
        lo.Length = self._channel_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(fast, mid, slow, hi, lo, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, mid)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, fast_val, mid_val, slow_val, hi_val, lo_val):
        if candle.State != CandleStates.Finished:
            return

        fast = float(fast_val)
        mid = float(mid_val)
        slow = float(slow_val)
        upper = float(hi_val)
        lower = float(lo_val)
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        # Check exits
        if self.Position > 0:
            if self._take_price > 0 and high >= self._take_price:
                self.SellMarket()
                self._entry_price = 0
                self._stop_price = 0
                self._take_price = 0
            elif self._stop_price > 0 and low <= self._stop_price:
                self.SellMarket()
                self._entry_price = 0
                self._stop_price = 0
                self._take_price = 0
        elif self.Position < 0:
            if self._take_price > 0 and low <= self._take_price:
                self.BuyMarket()
                self._entry_price = 0
                self._stop_price = 0
                self._take_price = 0
            elif self._stop_price > 0 and high >= self._stop_price:
                self.BuyMarket()
                self._entry_price = 0
                self._stop_price = 0
                self._take_price = 0

        if not self._has_prev:
            self._prev_fast = fast
            self._prev_mid = mid
            self._prev_slow = slow
            self._has_prev = True
            return

        # Cross detection
        cross_up = fast > mid and fast > slow and (self._prev_fast <= self._prev_mid or self._prev_fast <= self._prev_slow)
        cross_down = fast < mid and fast < slow and (self._prev_fast >= self._prev_mid or self._prev_fast >= self._prev_slow)

        if cross_up and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._entry_price = close
            self._stop_price = lower if lower > 0 else 0
            self._take_price = upper if upper > 0 else 0
        elif cross_down and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._entry_price = close
            self._stop_price = upper if upper > 0 else 0
            self._take_price = lower if lower > 0 else 0

        self._prev_fast = fast
        self._prev_mid = mid
        self._prev_slow = slow

    def CreateClone(self):
        return triple_ma_channel_crossover_strategy()
