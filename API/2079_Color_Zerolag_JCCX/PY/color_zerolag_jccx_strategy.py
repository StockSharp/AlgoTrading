import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class color_zerolag_jccx_strategy(Strategy):
    def __init__(self):
        super(color_zerolag_jccx_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 8) \
            .SetDisplay("Fast MA", "Fast moving average period", "Moving Average")
        self._slow_period = self.Param("SlowPeriod", 21) \
            .SetDisplay("Slow MA", "Slow moving average period", "Moving Average")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for calculation", "General")
        self._initialized = False
        self._prev_fast = 0.0
        self._prev_slow = 0.0

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
        super(color_zerolag_jccx_strategy, self).OnReseted()
        self._initialized = False
        self._prev_fast = 0.0
        self._prev_slow = 0.0

    def OnStarted(self, time):
        super(color_zerolag_jccx_strategy, self).OnStarted(time)
        fast_ma = ExponentialMovingAverage()
        fast_ma.Length = self.fast_period
        slow_ma = ExponentialMovingAverage()
        slow_ma.Length = self.slow_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_ma, slow_ma, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ma)
            self.DrawIndicator(area, slow_ma)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return
        fast = float(fast)
        slow = float(slow)
        if not self._initialized:
            self._prev_fast = fast
            self._prev_slow = slow
            self._initialized = True
            return

        was_fast_above = self._prev_fast > self._prev_slow
        is_fast_above = fast > slow

        if not was_fast_above and is_fast_above and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif was_fast_above and not is_fast_above and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return color_zerolag_jccx_strategy()
