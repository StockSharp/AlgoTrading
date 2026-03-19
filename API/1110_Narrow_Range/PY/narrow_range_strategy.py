import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class narrow_range_strategy(Strategy):
    """
    Narrow Range: EMA crossover with RSI filter and time-based cooldown.
    """

    def __init__(self):
        super(narrow_range_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candles", "General")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._initialized = False
        self._cooldown_bars = 24
        self._bar_index = 0
        self._last_signal_bar = -1000000

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(narrow_range_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._initialized = False
        self._bar_index = 0
        self._last_signal_bar = -1000000

    def OnStarted(self, time):
        super(narrow_range_strategy, self).OnStarted(time)
        fast = ExponentialMovingAverage()
        fast.Length = 14
        slow = ExponentialMovingAverage()
        slow.Length = 40
        rsi = RelativeStrengthIndex()
        rsi.Length = 14
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, rsi, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_val, slow_val, rsi_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        fast = float(fast_val)
        slow = float(slow_val)
        rsi = float(rsi_val)
        self._bar_index += 1
        if not self._initialized:
            self._prev_fast = fast
            self._prev_slow = slow
            self._initialized = True
            return
        can_signal = self._bar_index - self._last_signal_bar >= self._cooldown_bars
        if can_signal and self._prev_fast <= self._prev_slow and fast > slow and rsi > 50 and self.Position <= 0:
            self.BuyMarket()
            self._last_signal_bar = self._bar_index
        elif can_signal and self._prev_fast >= self._prev_slow and fast < slow and rsi < 50 and self.Position > 0:
            self.SellMarket()
            self._last_signal_bar = self._bar_index
        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return narrow_range_strategy()
