import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import SimpleMovingAverage

class x_bug_strategy(Strategy):
    def __init__(self):
        super(x_bug_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle type", "Primary timeframe used for signals.", "General")
        self._fast_period = self.Param("FastPeriod", 5) \
            .SetDisplay("Fast MA period", "Length of the fast moving average.", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 14) \
            .SetDisplay("Slow MA period", "Length of the slow moving average.", "Indicators")
        self._reverse_signals = self.Param("ReverseSignals", False) \
            .SetDisplay("Reverse signals", "Invert buy and sell directions.", "Trading")
        self._prev_fast = None
        self._prev_slow = None

    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def FastPeriod(self):
        return self._fast_period.Value
    @property
    def SlowPeriod(self):
        return self._slow_period.Value
    @property
    def ReverseSignals(self):
        return self._reverse_signals.Value

    def OnStarted2(self, time):
        super(x_bug_strategy, self).OnStarted2(time)
        from System import Decimal
        self.Volume = Decimal(0.001)
        self._prev_fast = None
        self._prev_slow = None
        self._slow_ma = SimpleMovingAverage()
        self._slow_ma.Length = self.SlowPeriod
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._slow_ma, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, slow_val):
        if candle.State != CandleStates.Finished:
            return
        if not self._slow_ma.IsFormed:
            return
        from System import Decimal
        sv = Decimal(float(slow_val))
        fast_value = candle.ClosePrice
        if self._prev_fast is None or self._prev_slow is None:
            self._prev_fast = fast_value
            self._prev_slow = sv
            return
        signal = 0
        if fast_value > sv and self._prev_fast <= self._prev_slow:
            signal = 1
        elif fast_value < sv and self._prev_fast >= self._prev_slow:
            signal = -1
        self._prev_fast = fast_value
        self._prev_slow = sv
        if signal == 0:
            return
        if self.ReverseSignals:
            signal = -signal
        if signal > 0 and self.Position <= 0:
            self.BuyMarket()
        elif signal < 0 and self.Position >= 0:
            self.SellMarket()

    def OnReseted(self):
        super(x_bug_strategy, self).OnReseted()
        self._prev_fast = None
        self._prev_slow = None

    def CreateClone(self):
        return x_bug_strategy()
