import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class color_zerolag_momentum_osma_strategy(Strategy):

    def __init__(self):
        super(color_zerolag_momentum_osma_strategy, self).__init__()

        self._fast_period = self.Param("FastPeriod", 8) \
            .SetDisplay("Fast Period", "Fast EMA period", "General")
        self._slow_period = self.Param("SlowPeriod", 21) \
            .SetDisplay("Slow Period", "Slow EMA period", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle Type", "General")

        self._prev_osma = 0.0
        self._prev_prev_osma = 0.0
        self._count = 0

    @property
    def FastPeriod(self):
        return self._fast_period.Value

    @FastPeriod.setter
    def FastPeriod(self, value):
        self._fast_period.Value = value

    @property
    def SlowPeriod(self):
        return self._slow_period.Value

    @SlowPeriod.setter
    def SlowPeriod(self, value):
        self._slow_period.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(color_zerolag_momentum_osma_strategy, self).OnStarted2(time)

        fast = ExponentialMovingAverage()
        fast.Length = self.FastPeriod
        slow = ExponentialMovingAverage()
        slow.Length = self.SlowPeriod

        self.SubscribeCandles(self.CandleType) \
            .Bind(fast, slow, self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return

        osma = float(fast_value) - float(slow_value)
        self._count += 1

        if self._count < 3:
            self._prev_prev_osma = self._prev_osma
            self._prev_osma = osma
            return

        turn_up = self._prev_osma < self._prev_prev_osma and osma > self._prev_osma
        turn_down = self._prev_osma > self._prev_prev_osma and osma < self._prev_osma

        if turn_up and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif turn_down and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_prev_osma = self._prev_osma
        self._prev_osma = osma

    def OnReseted(self):
        super(color_zerolag_momentum_osma_strategy, self).OnReseted()
        self._prev_osma = 0.0
        self._prev_prev_osma = 0.0
        self._count = 0

    def CreateClone(self):
        return color_zerolag_momentum_osma_strategy()
