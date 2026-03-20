import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class madx07_adx_ma_strategy(Strategy):
    def __init__(self):
        super(madx07_adx_ma_strategy, self).__init__()
        self._big_ma_period = self.Param("BigMaPeriod", 25) \
            .SetDisplay("Big MA Period", "Period of the slower MA", "MA")
        self._small_ma_period = self.Param("SmallMaPeriod", 10) \
            .SetDisplay("Small MA Period", "Period of the faster MA", "MA")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle type", "General")
        self._prev_small = 0.0
        self._prev_big = 0.0
        self._has_prev = False

    @property
    def big_ma_period(self):
        return self._big_ma_period.Value

    @property
    def small_ma_period(self):
        return self._small_ma_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(madx07_adx_ma_strategy, self).OnReseted()
        self._prev_small = 0.0
        self._prev_big = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(madx07_adx_ma_strategy, self).OnStarted(time)
        big_ma = ExponentialMovingAverage()
        big_ma.Length = self.big_ma_period
        small_ma = ExponentialMovingAverage()
        small_ma.Length = self.small_ma_period
        self.SubscribeCandles(self.candle_type).Bind(big_ma, small_ma, self.process_candle).Start()

    def process_candle(self, candle, big_ma_val, small_ma_val):
        if candle.State != CandleStates.Finished:
            return

        bv = float(big_ma_val)
        sv = float(small_ma_val)

        if not self._has_prev:
            self._prev_small = sv
            self._prev_big = bv
            self._has_prev = True
            return

        cross_up = self._prev_small <= self._prev_big and sv > bv
        cross_down = self._prev_small >= self._prev_big and sv < bv

        if cross_up and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif cross_down and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_small = sv
        self._prev_big = bv

    def CreateClone(self):
        return madx07_adx_ma_strategy()
