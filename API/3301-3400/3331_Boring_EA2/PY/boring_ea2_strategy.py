import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class boring_ea2_strategy(Strategy):
    def __init__(self):
        super(boring_ea2_strategy, self).__init__()

        self._fast = None
        self._med = None
        self._slow = None
        self._prev_fast = None
        self._prev_med = None

    def OnReseted(self):
        super(boring_ea2_strategy, self).OnReseted()
        self._fast = None
        self._med = None
        self._slow = None
        self._prev_fast = None
        self._prev_med = None

    def OnStarted2(self, time):
        super(boring_ea2_strategy, self).OnStarted2(time)

        self._fast = SimpleMovingAverage()
        self._fast.Length = 10
        self._med = SimpleMovingAverage()
        self._med.Length = 20
        self._slow = SimpleMovingAverage()
        self._slow.Length = 40

        subscription = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromMinutes(15)))
        subscription.Bind(self._fast, self._med, self._slow, self._process_candle)
        subscription.Start()

    def _process_candle(self, candle, fast_value, med_value, slow_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._fast.IsFormed or not self._med.IsFormed or not self._slow.IsFormed:
            return

        fast_val = float(fast_value)
        med_val = float(med_value)
        slow_val = float(slow_value)

        if self._prev_fast is not None and self._prev_med is not None:
            fast_cross_up = self._prev_fast <= self._prev_med and fast_val > med_val
            fast_cross_down = self._prev_fast >= self._prev_med and fast_val < med_val

            if fast_cross_up and med_val > slow_val and self.Position <= 0:
                self.BuyMarket()
            elif fast_cross_down and med_val < slow_val and self.Position >= 0:
                self.SellMarket()

        self._prev_fast = fast_val
        self._prev_med = med_val

    def CreateClone(self):
        return boring_ea2_strategy()
