import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import (
    ExponentialMovingAverage, RelativeStrengthIndex
)
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

class afl_winner_sign_strategy(Strategy):

    def __init__(self):
        super(afl_winner_sign_strategy, self).__init__()

        self._period = self.Param("Period", 10) \
            .SetDisplay("Stoch Period", "Base period for oscillator calculation", "AFL WinnerSign")
        self._k_period = self.Param("KPeriod", 5) \
            .SetDisplay("%K Period", "Smoothing period for %K line", "AFL WinnerSign")
        self._d_period = self.Param("DPeriod", 5) \
            .SetDisplay("%D Period", "Smoothing period for %D line", "AFL WinnerSign")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._fast = None
        self._slow = None
        self._prev_k = 0.0
        self._prev_d = 0.0
        self._is_initialized = False

    @property
    def Period(self):
        return self._period.Value

    @Period.setter
    def Period(self, value):
        self._period.Value = value

    @property
    def KPeriod(self):
        return self._k_period.Value

    @KPeriod.setter
    def KPeriod(self, value):
        self._k_period.Value = value

    @property
    def DPeriod(self):
        return self._d_period.Value

    @DPeriod.setter
    def DPeriod(self, value):
        self._d_period.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(afl_winner_sign_strategy, self).OnStarted2(time)

        self._fast = ExponentialMovingAverage()
        self._fast.Length = self.KPeriod
        self._slow = ExponentialMovingAverage()
        self._slow.Length = self.DPeriod

        rsi = RelativeStrengthIndex()
        rsi.Length = self.Period

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, self.ProcessCandle).Start()

        self.StartProtection(
            Unit(2, UnitTypes.Percent),
            Unit(2, UnitTypes.Percent))

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, momentum):
        if candle.State != CandleStates.Finished:
            return

        k_result = process_float(self._fast, float(momentum), candle.OpenTime, True)
        k = float(k_result)

        d_result = process_float(self._slow, k, candle.OpenTime, True)
        d = float(d_result)

        if not self._fast.IsFormed or not self._slow.IsFormed:
            return

        if not self._is_initialized:
            self._prev_k = k
            self._prev_d = d
            self._is_initialized = True
            return

        if self._prev_k <= self._prev_d and k > d and k < 35.0 and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif self._prev_k >= self._prev_d and k < d and k > 65.0 and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

        self._prev_k = k
        self._prev_d = d

    def OnReseted(self):
        super(afl_winner_sign_strategy, self).OnReseted()
        if self._fast is not None:
            self._fast.Length = self.KPeriod
            self._fast.Reset()
        if self._slow is not None:
            self._slow.Length = self.DPeriod
            self._slow.Reset()
        self._prev_k = 0.0
        self._prev_d = 0.0
        self._is_initialized = False

    def CreateClone(self):
        return afl_winner_sign_strategy()
