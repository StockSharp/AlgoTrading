import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class x_trail_strategy(Strategy):
    def __init__(self):
        super(x_trail_strategy, self).__init__()
        self._ma1_length = self.Param("Ma1Length", 10) \
            .SetDisplay("Fast MA Length", "Length of the fast moving average", "General")
        self._ma2_length = self.Param("Ma2Length", 30) \
            .SetDisplay("Slow MA Length", "Length of the slow moving average", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    @property
    def ma1_length(self):
        return self._ma1_length.Value

    @property
    def ma2_length(self):
        return self._ma2_length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(x_trail_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(x_trail_strategy, self).OnStarted2(time)
        ma1 = SimpleMovingAverage()
        ma1.Length = self.ma1_length
        ma2 = SimpleMovingAverage()
        ma2.Length = self.ma2_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ma1, ma2, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma1)
            self.DrawIndicator(area, ma2)
            self.DrawOwnTrades(area)

    def on_process(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return
        if self._has_prev:
            cross_up = self._prev_fast <= self._prev_slow and fast > slow
            cross_down = self._prev_fast >= self._prev_slow and fast < slow
            if cross_up and self.Position <= 0:
                self.BuyMarket()
            elif cross_down and self.Position >= 0:
                self.SellMarket()
        self._prev_fast = fast
        self._prev_slow = slow
        self._has_prev = True

    def CreateClone(self):
        return x_trail_strategy()
