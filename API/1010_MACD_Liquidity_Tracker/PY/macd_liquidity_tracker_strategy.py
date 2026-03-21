import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class macd_liquidity_tracker_strategy(Strategy):
    """
    MACD Liquidity Tracker: fast/slow EMA crossover with cooldown.
    """

    def __init__(self):
        super(macd_liquidity_tracker_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 12).SetDisplay("Fast", "Fast EMA", "MACD")
        self._slow_length = self.Param("SlowLength", 26).SetDisplay("Slow", "Slow EMA", "MACD")
        self._cooldown_bars = self.Param("CooldownBars", 10).SetDisplay("Cooldown", "Bars between signals", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candles", "General")

        self._prev_above = False
        self._is_init = False
        self._bars_from_signal = 9999

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(macd_liquidity_tracker_strategy, self).OnReseted()
        self._prev_above = False
        self._is_init = False
        self._bars_from_signal = 9999

    def OnStarted(self, time):
        super(macd_liquidity_tracker_strategy, self).OnStarted(time)
        fast = ExponentialMovingAverage()
        fast.Length = self._fast_length.Value
        slow = ExponentialMovingAverage()
        slow.Length = self._slow_length.Value
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
        if fast == 0 or slow == 0:
            return
        is_above = fast > slow
        if not self._is_init:
            self._prev_above = is_above
            self._is_init = True
            self._bars_from_signal = 0
            return
        self._bars_from_signal += 1
        cross_up = is_above and not self._prev_above
        cross_down = not is_above and self._prev_above
        can_signal = self._bars_from_signal >= self._cooldown_bars.Value
        if can_signal and cross_up and self.Position <= 0:
            self.BuyMarket()
            self._bars_from_signal = 0
        elif can_signal and cross_down and self.Position >= 0:
            self.SellMarket()
            self._bars_from_signal = 0
        self._prev_above = is_above

    def CreateClone(self):
        return macd_liquidity_tracker_strategy()
