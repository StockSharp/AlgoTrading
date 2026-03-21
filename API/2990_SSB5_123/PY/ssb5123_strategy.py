import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class ssb5123_strategy(Strategy):
    def __init__(self):
        super(ssb5123_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._fast_period = self.Param("FastPeriod", 5) \
            .SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._mid_period = self.Param("MidPeriod", 12) \
            .SetDisplay("Mid EMA", "Middle EMA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 26) \
            .SetDisplay("Slow EMA", "Slow EMA period", "Indicators")

        self._prev_fast = None
        self._prev_mid = None

    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def FastPeriod(self):
        return self._fast_period.Value
    @property
    def MidPeriod(self):
        return self._mid_period.Value
    @property
    def SlowPeriod(self):
        return self._slow_period.Value

    def OnReseted(self):
        super(ssb5123_strategy, self).OnReseted()
        self._prev_fast = None
        self._prev_mid = None

    def OnStarted(self, time):
        super(ssb5123_strategy, self).OnStarted(time)
        self._prev_fast = None
        self._prev_mid = None

        fast = ExponentialMovingAverage()
        fast.Length = self.FastPeriod
        mid = ExponentialMovingAverage()
        mid.Length = self.MidPeriod
        slow = ExponentialMovingAverage()
        slow.Length = self.SlowPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast, mid, slow, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, mid)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, fast_value, mid_value, slow_value):
        if candle.State != CandleStates.Finished:
            return
        fv = float(fast_value)
        mv = float(mid_value)
        sv = float(slow_value)
        if self._prev_fast is None or self._prev_mid is None:
            self._prev_fast = fv
            self._prev_mid = mv
            return
        # Fast crosses above mid while both above slow
        if self._prev_fast <= self._prev_mid and fv > mv and mv > sv and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        # Fast crosses below mid while both below slow
        elif self._prev_fast >= self._prev_mid and fv < mv and mv < sv and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        self._prev_fast = fv
        self._prev_mid = mv

    def CreateClone(self):
        return ssb5123_strategy()
