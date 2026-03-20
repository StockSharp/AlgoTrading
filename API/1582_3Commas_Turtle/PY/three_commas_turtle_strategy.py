import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class three_commas_turtle_strategy(Strategy):
    def __init__(self):
        super(three_commas_turtle_strategy, self).__init__()
        self._period_fast = self.Param("PeriodFast", 10) \
            .SetDisplay("Period Fast", "Fast channel period", "Channels")
        self._period_slow = self.Param("PeriodSlow", 15) \
            .SetDisplay("Period Slow", "Slow channel period", "Channels")
        self._period_exit = self.Param("PeriodExit", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Period Exit", "Exit channel period", "Channels")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_upper_fast = 0.0
        self._prev_lower_fast = 0.0
        self._prev_close = 0.0

    @property
    def period_fast(self):
        return self._period_fast.Value

    @property
    def period_slow(self):
        return self._period_slow.Value

    @property
    def period_exit(self):
        return self._period_exit.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(three_commas_turtle_strategy, self).OnReseted()
        self._prev_upper_fast = 0.0
        self._prev_lower_fast = 0.0
        self._prev_close = 0.0

    def OnStarted(self, time):
        super(three_commas_turtle_strategy, self).OnStarted(time)
        upper_fast = Highest()
        upper_fast.Length = self.period_fast
        lower_fast = Lowest()
        lower_fast.Length = self.period_fast
        upper_slow = Highest()
        upper_slow.Length = self.period_slow
        lower_slow = Lowest()
        lower_slow.Length = self.period_slow
        upper_exit = Highest()
        upper_exit.Length = self.period_exit
        lower_exit = Lowest()
        lower_exit.Length = self.period_exit
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(upper_fast, lower_fast, upper_slow, lower_slow, upper_exit, lower_exit, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, up_fast, low_fast, up_slow, low_slow, up_exit, low_exit):
        if candle.State != CandleStates.Finished:
            return
        if self._prev_close == 0:
            self._prev_upper_fast = up_fast
            self._prev_lower_fast = low_fast
            self._prev_close = candle.ClosePrice
            return
        if candle.ClosePrice > self._prev_upper_fast and self.Position <= 0:
            self.BuyMarket()
        elif candle.ClosePrice < self._prev_lower_fast and self.Position >= 0:
            self.SellMarket()
        elif self.Position > 0 and candle.ClosePrice < low_exit:
            self.SellMarket()
        elif self.Position < 0 and candle.ClosePrice > up_exit:
            self.BuyMarket()
        self._prev_upper_fast = up_fast
        self._prev_lower_fast = low_fast
        self._prev_close = candle.ClosePrice

    def CreateClone(self):
        return three_commas_turtle_strategy()
