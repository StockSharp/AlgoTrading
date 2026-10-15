import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WilliamsR
from StockSharp.Algo.Strategies import Strategy


class delta_wpr_strategy(Strategy):
    def __init__(self):
        super(delta_wpr_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 14) \
            .SetDisplay("Fast WPR Period", "Period for the fast Williams %R", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 30) \
            .SetDisplay("Slow WPR Period", "Period for the slow Williams %R", "Indicators")
        self._level = self.Param("Level", -50.0) \
            .SetDisplay("Signal Level", "Threshold level for signals", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_signal = 1

    @property
    def fast_period(self):
        return self._fast_period.Value

    @property
    def slow_period(self):
        return self._slow_period.Value

    @property
    def level(self):
        return self._level.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(delta_wpr_strategy, self).OnReseted()
        self._prev_signal = 1

    def OnStarted2(self, time):
        super(delta_wpr_strategy, self).OnStarted2(time)
        self._prev_signal = 1
        fast = WilliamsR()
        fast.Length = int(self.fast_period)
        slow = WilliamsR()
        slow.Length = int(self.slow_period)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, self.process_candle).Start()

    def process_candle(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return
        fast_value = float(fast_value)
        slow_value = float(slow_value)
        lvl = float(self.level)
        signal = 1
        if slow_value > lvl and fast_value > slow_value:
            signal = 0
        elif slow_value < lvl and fast_value < slow_value:
            signal = 2
        if signal == self._prev_signal:
            return
        if signal == 0 and self.Position <= 0:
            self.BuyMarket()
        elif signal == 2 and self.Position >= 0:
            self.SellMarket()
        self._prev_signal = signal

    def CreateClone(self):
        return delta_wpr_strategy()
