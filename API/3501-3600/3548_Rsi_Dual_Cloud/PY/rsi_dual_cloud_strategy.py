import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class rsi_dual_cloud_strategy(Strategy):
    def __init__(self):
        super(rsi_dual_cloud_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._fast_length = self.Param("FastLength", 14)
        self._slow_length = self.Param("SlowLength", 42)

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def FastLength(self):
        return self._fast_length.Value

    @FastLength.setter
    def FastLength(self, value):
        self._fast_length.Value = value

    @property
    def SlowLength(self):
        return self._slow_length.Value

    @SlowLength.setter
    def SlowLength(self, value):
        self._slow_length.Value = value

    def OnReseted(self):
        super(rsi_dual_cloud_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(rsi_dual_cloud_strategy, self).OnStarted2(time)
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

        fast_rsi = RelativeStrengthIndex()
        fast_rsi.Length = self.FastLength
        slow_rsi = RelativeStrengthIndex()
        slow_rsi.Length = self.SlowLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast_rsi, slow_rsi, self._process_candle).Start()

    def _process_candle(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return

        fast_val = float(fast_value)
        slow_val = float(slow_value)

        if self._has_prev:
            cross_up = self._prev_fast < self._prev_slow and fast_val > slow_val
            cross_down = self._prev_fast > self._prev_slow and fast_val < slow_val
            min_spread = 5.0

            if cross_up and abs(fast_val - slow_val) >= min_spread and self.Position <= 0:
                self.BuyMarket()
            elif cross_down and abs(fast_val - slow_val) >= min_spread and self.Position >= 0:
                self.SellMarket()

        self._prev_fast = fast_val
        self._prev_slow = slow_val
        self._has_prev = True

    def CreateClone(self):
        return rsi_dual_cloud_strategy()
