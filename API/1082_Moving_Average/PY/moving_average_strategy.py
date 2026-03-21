import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class moving_average_strategy(Strategy):
    """
    Moving average crossover: fast/slow EMA cross with cooldown.
    """

    def __init__(self):
        super(moving_average_strategy, self).__init__()
        self._short_length = self.Param("ShortLength", 6).SetDisplay("Fast", "Fast EMA", "Indicators")
        self._long_length = self.Param("LongLength", 21).SetDisplay("Slow", "Slow EMA", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 50).SetDisplay("Cooldown", "Min bars between entries", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candles", "General")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._initialized = False
        self._bar_index = 0
        self._last_trade_bar = -1000000

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(moving_average_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._initialized = False
        self._bar_index = 0
        self._last_trade_bar = -1000000

    def OnStarted(self, time):
        super(moving_average_strategy, self).OnStarted(time)
        fast = ExponentialMovingAverage()
        fast.Length = self._short_length.Value
        slow = ExponentialMovingAverage()
        slow.Length = self._long_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return
        fast = float(fast_val)
        slow = float(slow_val)
        self._bar_index += 1
        if not self._initialized:
            self._prev_fast = fast
            self._prev_slow = slow
            self._initialized = True
            return
        can_trade = self._bar_index - self._last_trade_bar >= self._cooldown_bars.Value
        if can_trade and self._prev_fast < self._prev_slow and fast >= slow and self.Position <= 0:
            self.BuyMarket()
            self._last_trade_bar = self._bar_index
        elif can_trade and self._prev_fast >= self._prev_slow and fast < slow and self.Position > 0:
            self.SellMarket()
            self._last_trade_bar = self._bar_index
        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return moving_average_strategy()
