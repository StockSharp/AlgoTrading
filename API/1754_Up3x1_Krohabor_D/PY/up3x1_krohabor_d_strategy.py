import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class up3x1_krohabor_d_strategy(Strategy):
    def __init__(self):
        super(up3x1_krohabor_d_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 12) \
            .SetDisplay("Fast Period", "Fast EMA period", "MA Settings")
        self._middle_period = self.Param("MiddlePeriod", 26) \
            .SetDisplay("Middle Period", "Middle EMA period", "MA Settings")
        self._slow_period = self.Param("SlowPeriod", 50) \
            .SetDisplay("Slow Period", "Slow EMA period", "MA Settings")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_fast = 0.0
        self._prev_middle = 0.0
        self._is_initialized = False

    @property
    def fast_period(self):
        return self._fast_period.Value

    @property
    def middle_period(self):
        return self._middle_period.Value

    @property
    def slow_period(self):
        return self._slow_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(up3x1_krohabor_d_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_middle = 0.0
        self._is_initialized = False

    def OnStarted(self, time):
        super(up3x1_krohabor_d_strategy, self).OnStarted(time)
        fast_ma = ExponentialMovingAverage()
        fast_ma.Length = self.fast_period
        middle_ma = ExponentialMovingAverage()
        middle_ma.Length = self.middle_period
        slow_ma = ExponentialMovingAverage()
        slow_ma.Length = self.slow_period
        self.SubscribeCandles(self.candle_type).Bind(fast_ma, middle_ma, slow_ma, self.process_candle).Start()

    def process_candle(self, candle, fast, middle, slow):
        if candle.State != CandleStates.Finished:
            return

        fv = float(fast)
        mv = float(middle)

        if not self._is_initialized:
            self._prev_fast = fv
            self._prev_middle = mv
            self._is_initialized = True
            return

        cross_up = self._prev_fast <= self._prev_middle and fv > mv
        cross_down = self._prev_fast >= self._prev_middle and fv < mv

        self._prev_fast = fv
        self._prev_middle = mv

        if cross_up and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif cross_down and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

    def CreateClone(self):
        return up3x1_krohabor_d_strategy()
