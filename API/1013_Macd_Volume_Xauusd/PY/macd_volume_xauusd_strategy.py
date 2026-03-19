import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class macd_volume_xauusd_strategy(Strategy):
    """
    MACD Volume XAUUSD: EMA crossover with cooldown (simplified from C# volume-based version).
    """

    def __init__(self):
        super(macd_volume_xauusd_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 10).SetDisplay("Fast", "Fast EMA", "Indicators")
        self._slow_length = self.Param("SlowLength", 20).SetDisplay("Slow", "Slow EMA", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 2).SetDisplay("Cooldown", "Bars between signals", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candles", "General")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._initialized = False
        self._bars_from_signal = 9999

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(macd_volume_xauusd_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._initialized = False
        self._bars_from_signal = 9999

    def OnStarted(self, time):
        super(macd_volume_xauusd_strategy, self).OnStarted(time)
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
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        fast = float(fast_val)
        slow = float(slow_val)
        if not self._initialized:
            self._prev_fast = fast
            self._prev_slow = slow
            self._initialized = True
            return
        self._bars_from_signal += 1
        cross_up = self._prev_fast <= 0 and fast > 0
        cross_down = self._prev_fast >= 0 and fast < 0
        diff = fast - slow
        prev_diff = self._prev_fast - self._prev_slow
        cross_up = prev_diff <= 0 and diff > 0
        cross_down = prev_diff >= 0 and diff < 0
        can_signal = self._bars_from_signal >= self._cooldown_bars.Value
        if can_signal and cross_up and self.Position <= 0:
            self.BuyMarket()
            self._bars_from_signal = 0
        elif can_signal and cross_down and self.Position >= 0:
            self.SellMarket()
            self._bars_from_signal = 0
        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return macd_volume_xauusd_strategy()
