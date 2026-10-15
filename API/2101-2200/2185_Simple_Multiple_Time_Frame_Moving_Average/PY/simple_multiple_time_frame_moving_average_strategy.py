import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class simple_multiple_time_frame_moving_average_strategy(Strategy):
    def __init__(self):
        super(simple_multiple_time_frame_moving_average_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 5) \
            .SetDisplay("Fast MA", "Fast moving average period", "General")
        self._slow_length = self.Param("SlowLength", 20) \
            .SetDisplay("Slow MA", "Slow moving average period", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for candles", "General")
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
        super(simple_multiple_time_frame_moving_average_strategy, self).OnReseted()
        self._prev_fast = None
        self._prev_slow = None

    def OnStarted2(self, time):
        super(simple_multiple_time_frame_moving_average_strategy, self).OnStarted2(time)

        fast_sma = ExponentialMovingAverage()
        fast_sma.Length = self.fast_length
        slow_sma = ExponentialMovingAverage()
        slow_sma.Length = self.slow_length

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_sma, slow_sma, self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_sma)
            self.DrawIndicator(area, slow_sma)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return

        f = float(fast)
        s = float(slow)

        if self._prev_fast is not None and self._prev_slow is not None:
            fast_up = f > self._prev_fast
            fast_down = f < self._prev_fast
            slow_up = s > self._prev_slow
            slow_down = s < self._prev_slow

            if fast_up and slow_up and self.Position <= 0:
                self.BuyMarket()
            elif fast_down and slow_down and self.Position >= 0:
                self.SellMarket()

        self._prev_fast = f
        self._prev_slow = s

    def CreateClone(self):
        return simple_multiple_time_frame_moving_average_strategy()
