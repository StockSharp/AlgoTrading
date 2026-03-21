import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class multi_conditions_curve_fitting_strategy(Strategy):
    """
    Multi conditions curve fitting: EMA crossover with RSI filter.
    """

    def __init__(self):
        super(multi_conditions_curve_fitting_strategy, self).__init__()
        self._fast_length = self.Param("FastEmaLength", 10).SetDisplay("Fast EMA", "Fast EMA", "Indicators")
        self._slow_length = self.Param("SlowEmaLength", 25).SetDisplay("Slow EMA", "Slow EMA", "Indicators")
        self._rsi_length = self.Param("RsiLength", 14).SetDisplay("RSI", "RSI period", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 5).SetDisplay("Cooldown", "Min bars between entries", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candles", "General")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False
        self._bar_index = 0
        self._last_signal_bar = -1000000

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(multi_conditions_curve_fitting_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False
        self._bar_index = 0
        self._last_signal_bar = -1000000

    def OnStarted(self, time):
        super(multi_conditions_curve_fitting_strategy, self).OnStarted(time)
        fast = ExponentialMovingAverage()
        fast.Length = self._fast_length.Value
        slow = ExponentialMovingAverage()
        slow.Length = self._slow_length.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
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
        fast = float(fast_val)
        slow = float(slow_val)
        rsi = float(rsi_val)
        self._bar_index += 1
        if not self._has_prev:
            self._prev_fast = fast
            self._prev_slow = slow
            self._has_prev = True
            return
        can_signal = self._bar_index - self._last_signal_bar >= self._cooldown_bars.Value
        long_signal = self._prev_fast <= self._prev_slow and fast > slow and rsi <= 60.0
        short_signal = self._prev_fast >= self._prev_slow and fast < slow and rsi >= 40.0
        if can_signal and long_signal and self.Position <= 0:
            self.BuyMarket()
            self._last_signal_bar = self._bar_index
        elif can_signal and short_signal and self.Position >= 0:
            self.SellMarket()
            self._last_signal_bar = self._bar_index
        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return multi_conditions_curve_fitting_strategy()
