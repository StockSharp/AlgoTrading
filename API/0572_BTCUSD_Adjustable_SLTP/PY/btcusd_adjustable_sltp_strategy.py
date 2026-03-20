import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class btcusd_adjustable_sltp_strategy(Strategy):
    def __init__(self):
        super(btcusd_adjustable_sltp_strategy, self).__init__()
        self._fast_sma_length = self.Param("FastSmaLength", 120) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast SMA", "Length of fast SMA", "Indicators")
        self._slow_sma_length = self.Param("SlowSmaLength", 450) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow SMA", "Length of slow SMA", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_fast = 0.0
        self._prev_slow = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(btcusd_adjustable_sltp_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0

    def OnStarted(self, time):
        super(btcusd_adjustable_sltp_strategy, self).OnStarted(time)
        fast_sma = SimpleMovingAverage()
        fast_sma.Length = self._fast_sma_length.Value
        slow_sma = SimpleMovingAverage()
        slow_sma.Length = self._slow_sma_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_sma, slow_sma, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_sma)
            self.DrawIndicator(area, slow_sma)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return
        fast_v = float(fast_val)
        slow_v = float(slow_val)
        if self._prev_fast == 0 or self._prev_slow == 0:
            self._prev_fast = fast_v
            self._prev_slow = slow_v
            return
        if self._prev_fast <= self._prev_slow and fast_v > slow_v and self.Position <= 0:
            self.BuyMarket()
        elif self._prev_fast >= self._prev_slow and fast_v < slow_v and self.Position >= 0:
            self.SellMarket()
        self._prev_fast = fast_v
        self._prev_slow = slow_v

    def CreateClone(self):
        return btcusd_adjustable_sltp_strategy()
