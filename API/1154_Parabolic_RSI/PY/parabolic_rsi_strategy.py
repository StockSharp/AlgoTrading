import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class parabolic_rsi_strategy(Strategy):
    def __init__(self):
        super(parabolic_rsi_strategy, self).__init__()
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetGreaterThanZero()
        self._ema_length = self.Param("EmaLength", 40) \
            .SetGreaterThanZero()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._initialized = False
        self._last_signal_ticks = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(parabolic_rsi_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._initialized = False
        self._last_signal_ticks = 0

    def OnStarted2(self, time):
        super(parabolic_rsi_strategy, self).OnStarted2(time)
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._initialized = False
        self._last_signal_ticks = 0
        self._fast = ExponentialMovingAverage()
        self._fast.Length = 14
        self._slow = ExponentialMovingAverage()
        self._slow.Length = self._ema_length.Value
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
        cooldown_ticks = TimeSpan.FromMinutes(360).Ticks
        current_ticks = candle.OpenTime.Ticks
        if current_ticks - self._last_signal_ticks >= cooldown_ticks:
            if self._prev_fast <= self._prev_slow and fv > sv and rv > 50 and self.Position <= 0:
                self.BuyMarket()
                self._last_signal_ticks = current_ticks
            elif self._prev_fast >= self._prev_slow and fv < sv and rv < 50 and self.Position >= 0:
                self.SellMarket()
                self._last_signal_ticks = current_ticks
        self._prev_fast = fv
        self._prev_slow = sv

    def CreateClone(self):
        return parabolic_rsi_strategy()
