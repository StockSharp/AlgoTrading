import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WeightedMovingAverage, KaufmanAdaptiveMovingAverage
from StockSharp.Algo.Strategies import Strategy


class snowieso_strategy(Strategy):
    def __init__(self):
        super(snowieso_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 10) \
            .SetDisplay("Fast WMA", "Fast WMA period", "Indicators")
        self._slow_length = self.Param("SlowLength", 20) \
            .SetDisplay("Slow WMA", "Slow WMA period", "Indicators")
        self._kama_length = self.Param("KamaLength", 10) \
            .SetDisplay("KAMA Length", "KAMA period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._prev_kama = 0.0
        self._has_prev = False

    @property
    def fast_length(self):
        return self._fast_length.Value

    @property
    def slow_length(self):
        return self._slow_length.Value

    @property
    def kama_length(self):
        return self._kama_length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(snowieso_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._prev_kama = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(snowieso_strategy, self).OnStarted(time)
        fast = WeightedMovingAverage()
        fast.Length = self.fast_length
        slow = WeightedMovingAverage()
        slow.Length = self.slow_length
        kama = KaufmanAdaptiveMovingAverage()
        kama.Length = self.kama_length
        self.SubscribeCandles(self.candle_type).Bind(fast, slow, kama, self.process_candle).Start()

    def process_candle(self, candle, fast_value, slow_value, kama_value):
        if candle.State != CandleStates.Finished:
            return

        fv = float(fast_value)
        sv = float(slow_value)
        kv = float(kama_value)

        if not self._has_prev:
            self._prev_fast = fv
            self._prev_slow = sv
            self._prev_kama = kv
            self._has_prev = True
            return

        cross_up = self._prev_fast <= self._prev_slow and fv > sv
        cross_down = self._prev_fast >= self._prev_slow and fv < sv
        kama_rising = kv > self._prev_kama
        kama_falling = kv < self._prev_kama

        if cross_up and kama_rising and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif cross_down and kama_falling and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_fast = fv
        self._prev_slow = sv
        self._prev_kama = kv

    def CreateClone(self):
        return snowieso_strategy()
