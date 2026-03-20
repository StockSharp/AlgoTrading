import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy


class cci_woodies_strategy(Strategy):
    def __init__(self):
        super(cci_woodies_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 6) \
            .SetDisplay("Fast CCI Period", "Period for fast CCI", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 14) \
            .SetDisplay("Slow CCI Period", "Period for slow CCI", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._is_initialized = False

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
        super(cci_woodies_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._is_initialized = False

    def OnStarted(self, time):
        super(cci_woodies_strategy, self).OnStarted(time)
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._is_initialized = False
        fast_cci = CommodityChannelIndex()
        fast_cci.Length = self.fast_period
        slow_cci = CommodityChannelIndex()
        slow_cci.Length = self.slow_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_cci, slow_cci, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_cci)
            self.DrawIndicator(area, slow_cci)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return
        fast = float(fast)
        slow = float(slow)
        if not self._is_initialized:
            self._prev_fast = fast
            self._prev_slow = slow
            self._is_initialized = True
            return
        cross_down = self._prev_fast > self._prev_slow and fast <= slow
        cross_up = self._prev_fast < self._prev_slow and fast >= slow
        if cross_down and self.Position <= 0:
            self.BuyMarket()
        elif cross_up and self.Position >= 0:
            self.SellMarket()
        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return cci_woodies_strategy()
