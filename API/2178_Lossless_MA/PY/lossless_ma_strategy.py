import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class lossless_ma_strategy(Strategy):
    def __init__(self):
        super(lossless_ma_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 10) \
            .SetDisplay("Fast MA", "Fast SMA length", "Parameters")
        self._slow_length = self.Param("SlowLength", 30) \
            .SetDisplay("Slow MA", "Slow SMA length", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candles for strategy", "General")
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
        super(lossless_ma_strategy, self).OnReseted()
        self._prev_fast = None
        self._prev_slow = None

    def OnStarted(self, time):
        super(lossless_ma_strategy, self).OnStarted(time)

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

    def process_candle(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return

        f = float(fast_value)
        s = float(slow_value)

        if self._prev_fast is not None and self._prev_slow is not None:
            # Bullish crossover
            if self._prev_fast <= self._prev_slow and f > s and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()

            # Bearish crossover
            if self._prev_fast >= self._prev_slow and f < s and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()

        self._prev_fast = f
        self._prev_slow = s

    def CreateClone(self):
        return lossless_ma_strategy()
