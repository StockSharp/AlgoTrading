import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class multi_timeframe_rsi_grid_with_arrows_strategy(Strategy):
    def __init__(self):
        super(multi_timeframe_rsi_grid_with_arrows_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 10) \
            .SetGreaterThanZero()
        self._slow_length = self.Param("SlowLength", 30) \
            .SetGreaterThanZero()
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetGreaterThanZero()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._initialized = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(multi_timeframe_rsi_grid_with_arrows_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._initialized = False

    def OnStarted(self, time):
        super(multi_timeframe_rsi_grid_with_arrows_strategy, self).OnStarted(time)
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._initialized = False
        self._fast = ExponentialMovingAverage()
        self._fast.Length = self._fast_length.Value
        self._slow = ExponentialMovingAverage()
        self._slow.Length = self._slow_length.Value
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self._rsi_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._fast, self._slow, self._rsi, self.OnProcess).Start()

    def OnProcess(self, candle, f, s, r):
        if candle.State != CandleStates.Finished:
            return
        if not self._fast.IsFormed or not self._slow.IsFormed or not self._rsi.IsFormed:
            return
        fv = float(f)
        sv = float(s)
        rv = float(r)
        if not self._initialized:
            self._prev_fast = fv
            self._prev_slow = sv
            self._initialized = True
            return
        if self._prev_fast <= self._prev_slow and fv > sv and rv > 45.0 and self.Position <= 0:
            self.BuyMarket()
        elif self._prev_fast >= self._prev_slow and fv < sv and rv < 55.0 and self.Position > 0:
            self.SellMarket()
        self._prev_fast = fv
        self._prev_slow = sv

    def CreateClone(self):
        return multi_timeframe_rsi_grid_with_arrows_strategy()
