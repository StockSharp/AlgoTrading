import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class ma_with_logistic_strategy(Strategy):
    """
    MA crossover strategy with percent-based exits.
    """

    def __init__(self):
        super(ma_with_logistic_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 12).SetDisplay("Fast MA", "Fast MA period", "Indicators")
        self._slow_length = self.Param("SlowLength", 25).SetDisplay("Slow MA", "Slow MA period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(20))).SetDisplay("Candle Type", "Candles", "General")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._is_init = False
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ma_with_logistic_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._is_init = False
        self._cooldown = 0

    def OnStarted2(self, time):
        super(ma_with_logistic_strategy, self).OnStarted2(time)
        self._fast_ma = ExponentialMovingAverage()
        self._fast_ma.Length = self._fast_length.Value
        self._slow_ma = ExponentialMovingAverage()
        self._slow_ma.Length = self._slow_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._fast_ma, self._slow_ma, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._fast_ma)
            self.DrawIndicator(area, self._slow_ma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return
        if not self._fast_ma.IsFormed or not self._slow_ma.IsFormed:
            return
        fast = float(fast_val)
        slow = float(slow_val)
        if not self._is_init:
            self._prev_fast = fast
            self._prev_slow = slow
            self._is_init = True
            return
        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_fast = fast
            self._prev_slow = slow
            return
        cross_up = self._prev_fast <= self._prev_slow and fast > slow
        cross_down = self._prev_fast >= self._prev_slow and fast < slow
        if cross_up and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown = 5
        elif cross_down and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown = 5
        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return ma_with_logistic_strategy()
