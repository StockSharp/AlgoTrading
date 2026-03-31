import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class rsi_ma_on_rsi_dual_strategy(Strategy):
    def __init__(self):
        super(rsi_ma_on_rsi_dual_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._fast_rsi_period = self.Param("FastRsiPeriod", 14)
        self._slow_rsi_period = self.Param("SlowRsiPeriod", 28)
        self._ma_period = self.Param("MaPeriod", 12)

        self._fast_rsi_history = []
        self._slow_rsi_history = []
        self._prev_fast_ma = 0.0
        self._prev_slow_ma = 0.0
        self._has_prev = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def FastRsiPeriod(self):
        return self._fast_rsi_period.Value

    @FastRsiPeriod.setter
    def FastRsiPeriod(self, value):
        self._fast_rsi_period.Value = value

    @property
    def SlowRsiPeriod(self):
        return self._slow_rsi_period.Value

    @SlowRsiPeriod.setter
    def SlowRsiPeriod(self, value):
        self._slow_rsi_period.Value = value

    @property
    def MaPeriod(self):
        return self._ma_period.Value

    @MaPeriod.setter
    def MaPeriod(self, value):
        self._ma_period.Value = value

    def OnReseted(self):
        super(rsi_ma_on_rsi_dual_strategy, self).OnReseted()
        self._fast_rsi_history = []
        self._slow_rsi_history = []
        self._prev_fast_ma = 0.0
        self._prev_slow_ma = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(rsi_ma_on_rsi_dual_strategy, self).OnStarted2(time)
        self._fast_rsi_history = []
        self._slow_rsi_history = []
        self._prev_fast_ma = 0.0
        self._prev_slow_ma = 0.0
        self._has_prev = False

        fast_rsi = RelativeStrengthIndex()
        fast_rsi.Length = self.FastRsiPeriod
        slow_rsi = RelativeStrengthIndex()
        slow_rsi.Length = self.SlowRsiPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast_rsi, slow_rsi, self._process_candle).Start()

    def _process_candle(self, candle, fast_rsi_value, slow_rsi_value):
        if candle.State != CandleStates.Finished:
            return

        fast_val = float(fast_rsi_value)
        slow_val = float(slow_rsi_value)
        ma_len = self.MaPeriod

        self._fast_rsi_history.append(fast_val)
        self._slow_rsi_history.append(slow_val)
        while len(self._fast_rsi_history) > ma_len:
            self._fast_rsi_history.pop(0)
        while len(self._slow_rsi_history) > ma_len:
            self._slow_rsi_history.pop(0)

        if len(self._fast_rsi_history) < ma_len or len(self._slow_rsi_history) < ma_len:
            return

        fast_ma = sum(self._fast_rsi_history) / ma_len
        slow_ma = sum(self._slow_rsi_history) / ma_len

        if self._has_prev:
            cross_up = self._prev_fast_ma < self._prev_slow_ma and fast_ma > slow_ma
            cross_down = self._prev_fast_ma > self._prev_slow_ma and fast_ma < slow_ma

            if cross_up and self.Position <= 0:
                self.BuyMarket()
            elif cross_down and self.Position >= 0:
                self.SellMarket()

        self._prev_fast_ma = fast_ma
        self._prev_slow_ma = slow_ma
        self._has_prev = True

    def CreateClone(self):
        return rsi_ma_on_rsi_dual_strategy()
