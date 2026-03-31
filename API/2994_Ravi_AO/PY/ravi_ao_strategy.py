import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class ravi_ao_strategy(Strategy):
    def __init__(self):
        super(ravi_ao_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._fast_period = self.Param("FastPeriod", 7) \
            .SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 25) \
            .SetDisplay("Slow EMA", "Slow EMA period", "Indicators")

        self._prev_fast = None
        self._prev_slow = None

    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def FastPeriod(self):
        return self._fast_period.Value
    @property
    def SlowPeriod(self):
        return self._slow_period.Value

    def OnReseted(self):
        super(ravi_ao_strategy, self).OnReseted()
        self._prev_fast = None
        self._prev_slow = None

    def OnStarted2(self, time):
        super(ravi_ao_strategy, self).OnStarted2(time)
        self._prev_fast = None
        self._prev_slow = None

        fast = ExponentialMovingAverage()
        fast.Length = self.FastPeriod
        slow = ExponentialMovingAverage()
        slow.Length = self.SlowPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast, slow, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return
        fv = float(fast_value)
        sv = float(slow_value)
        if self._prev_fast is None or self._prev_slow is None:
            self._prev_fast = fv
            self._prev_slow = sv
            return
        prev_above = self._prev_fast > self._prev_slow
        curr_above = fv > sv
        self._prev_fast = fv
        self._prev_slow = sv
        if not prev_above and curr_above and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif prev_above and not curr_above and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

    def CreateClone(self):
        return ravi_ao_strategy()
