import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class hedge_average_strategy(Strategy):
    def __init__(self):
        super(hedge_average_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 5) \
            .SetDisplay("Fast Period", "Fast SMA period", "Parameters")
        self._slow_period = self.Param("SlowPeriod", 20) \
            .SetDisplay("Slow Period", "Slow SMA period", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_fast = None
        self._prev_slow = None

    @property
    def fast_period(self):
        return self._fast_period.Value

    @property
    def slow_period(self):
        return self._slow_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(hedge_average_strategy, self).OnReseted()
        self._prev_fast = None
        self._prev_slow = None

    def OnStarted2(self, time):
        super(hedge_average_strategy, self).OnStarted2(time)
        fast = ExponentialMovingAverage()
        fast.Length = self.fast_period
        slow = ExponentialMovingAverage()
        slow.Length = self.slow_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return
        fast_val = float(fast_val)
        slow_val = float(slow_val)
        if self._prev_fast is not None and self._prev_slow is not None:
            pf = self._prev_fast
            ps = self._prev_slow
            if pf <= ps and fast_val > slow_val and self.Position <= 0:
                self.BuyMarket()
            elif pf >= ps and fast_val < slow_val and self.Position >= 0:
                self.SellMarket()
        self._prev_fast = fast_val
        self._prev_slow = slow_val

    def CreateClone(self):
        return hedge_average_strategy()
