import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class bago_ea_classic_strategy(Strategy):
    def __init__(self):
        super(bago_ea_classic_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 5) \
            .SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 12) \
            .SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI period", "Indicators")
        self._rsi_level = self.Param("RsiLevel", 50.0) \
            .SetDisplay("RSI Level", "RSI neutral level", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    @property
    def fast_period(self):
        return self._fast_period.Value

    @property
    def slow_period(self):
        return self._slow_period.Value

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @property
    def rsi_level(self):
        return self._rsi_level.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(bago_ea_classic_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(bago_ea_classic_strategy, self).OnStarted(time)
        self._has_prev = False
        fast = ExponentialMovingAverage()
        fast.Length = self.fast_period
        slow = ExponentialMovingAverage()
        slow.Length = self.slow_period
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, rsi, self.process_candle).Start()

    def process_candle(self, candle, fast, slow, rsi):
        if candle.State != CandleStates.Finished:
            return
        fast_val = float(fast)
        slow_val = float(slow)
        rsi_val = float(rsi)
        if not self._has_prev:
            self._prev_fast = fast_val
            self._prev_slow = slow_val
            self._has_prev = True
            return
        cross_up = self._prev_fast <= self._prev_slow and fast_val > slow_val
        cross_down = self._prev_fast >= self._prev_slow and fast_val < slow_val
        if cross_up and rsi_val > self.rsi_level and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif cross_down and rsi_val < self.rsi_level and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        self._prev_fast = fast_val
        self._prev_slow = slow_val

    def CreateClone(self):
        return bago_ea_classic_strategy()
