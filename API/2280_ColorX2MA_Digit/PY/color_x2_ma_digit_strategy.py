import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class color_x2_ma_digit_strategy(Strategy):
    def __init__(self):
        super(color_x2_ma_digit_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 8) \
            .SetDisplay("Fast MA Length", "Length of the first smoothing", "Parameters")
        self._slow_length = self.Param("SlowLength", 21) \
            .SetDisplay("Slow MA Length", "Length of the second smoothing", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for strategy", "General")
        self._prev_fast = None
        self._prev_slow = None

    @property
    def fast_length(self):
        return self._fast_length.Value

    @property
    def slow_length(self):
        return self._slow_length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(color_x2_ma_digit_strategy, self).OnReseted()
        self._prev_fast = None
        self._prev_slow = None

    def OnStarted(self, time):
        super(color_x2_ma_digit_strategy, self).OnStarted(time)
        self._prev_fast = None
        self._prev_slow = None
        fast_ma = ExponentialMovingAverage()
        fast_ma.Length = self.fast_length
        slow_ma = ExponentialMovingAverage()
        slow_ma.Length = self.slow_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_ma, slow_ma, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ma)
            self.DrawIndicator(area, slow_ma)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, fast_ma, slow_ma):
        if candle.State != CandleStates.Finished:
            return
        fast_ma = float(fast_ma)
        slow_ma = float(slow_ma)
        if self._prev_fast is None or self._prev_slow is None:
            self._prev_fast = fast_ma
            self._prev_slow = slow_ma
            return
        was_above = self._prev_fast > self._prev_slow
        is_above = fast_ma > slow_ma
        if not was_above and is_above and self.Position <= 0:
            self.BuyMarket()
        elif was_above and not is_above and self.Position >= 0:
            self.SellMarket()
        self._prev_fast = fast_ma
        self._prev_slow = slow_ma

    def CreateClone(self):
        return color_x2_ma_digit_strategy()
