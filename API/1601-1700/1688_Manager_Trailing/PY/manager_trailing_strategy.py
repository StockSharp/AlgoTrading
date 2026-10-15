import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class manager_trailing_strategy(Strategy):
    def __init__(self):
        super(manager_trailing_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 8) \
            .SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 21) \
            .SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "ATR period for trailing", "Indicators")
        self._trail_mult = self.Param("TrailMult", 2.0) \
            .SetDisplay("Trail Mult", "ATR multiplier for trailing stop", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False
        self._trail_stop = 0.0
        self._high_since_long = 0.0
        self._low_since_short = 1e18

    @property
    def fast_period(self):
        return self._fast_period.Value

    @property
    def slow_period(self):
        return self._slow_period.Value

    @property
    def atr_period(self):
        return self._atr_period.Value

    @property
    def trail_mult(self):
        return self._trail_mult.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(manager_trailing_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False
        self._trail_stop = 0.0
        self._high_since_long = 0.0
        self._low_since_short = 1e18

    def OnStarted2(self, time):
        super(manager_trailing_strategy, self).OnStarted2(time)
        fast = ExponentialMovingAverage()
        fast.Length = self.fast_period
        slow = ExponentialMovingAverage()
        slow.Length = self.slow_period
        atr = StandardDeviation()
        atr.Length = self.atr_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, atr, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, fast, slow, atr):
        if candle.State != CandleStates.Finished:
            return
        if not self._has_prev:
            self._prev_fast = fast
            self._prev_slow = slow
            self._has_prev = True
            return
        close = candle.ClosePrice
        # Trail stop management
        if self.Position > 0 and atr > 0:
            self._high_since_long = max(self._high_since_long, float(candle.HighPrice))
            new_trail = self._high_since_long - atr * self.trail_mult
            if new_trail > self._trail_stop:
                self._trail_stop = new_trail
            if close <= self._trail_stop:
                self.SellMarket()
                self._trail_stop = 0
                self._prev_fast = fast
                self._prev_slow = slow
                return
        elif self.Position < 0 and atr > 0:
            self._low_since_short = min(self._low_since_short, float(candle.LowPrice))
            new_trail = self._low_since_short + atr * self.trail_mult
            if new_trail < self._trail_stop or self._trail_stop == 0:
                self._trail_stop = new_trail
            if close >= self._trail_stop:
                self.BuyMarket()
                self._trail_stop = 0
                self._prev_fast = fast
                self._prev_slow = slow
                return
        # Entry signals: EMA crossover
        if self._prev_fast <= self._prev_slow and fast > slow:
            if self.Position < 0:
                self.BuyMarket()
            if self.Position <= 0:
                self.BuyMarket()
                self._high_since_long = float(candle.HighPrice)
                self._trail_stop = close - atr * self.trail_mult
        elif self._prev_fast >= self._prev_slow and fast < slow:
            if self.Position > 0:
                self.SellMarket()
            if self.Position >= 0:
                self.SellMarket()
                self._low_since_short = float(candle.LowPrice)
                self._trail_stop = close + atr * self.trail_mult
        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return manager_trailing_strategy()
