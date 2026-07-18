import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class mvo_ma_signal_strategy(Strategy):
    def __init__(self):
        super(mvo_ma_signal_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15)))
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
        super(mvo_ma_signal_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._initialized = False

    def OnStarted2(self, time):
        super(mvo_ma_signal_strategy, self).OnStarted2(time)
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._initialized = False
        self._fast = ExponentialMovingAverage()
        self._fast.Length = 12
        self._slow = ExponentialMovingAverage()
        self._slow.Length = 26
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = 14
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
        return mvo_ma_signal_strategy()
