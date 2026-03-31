import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import HullMovingAverage, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class mh_hull_moving_average_based_trading_strategy(Strategy):
    def __init__(self):
        super(mh_hull_moving_average_based_trading_strategy, self).__init__()
        self._hull_period = self.Param("HullPeriod", 120) \
            .SetGreaterThanZero() \
            .SetDisplay("Hull Period", "Period for Hull Moving Average", "Indicators")
        self._signal_threshold_percent = self.Param("SignalThresholdPercent", 0.15) \
            .SetGreaterThanZero() \
            .SetDisplay("Signal Threshold %", "Minimum distance from HMA", "Indicators")
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Signal Cooldown Bars", "Minimum bars between entries", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_diff_percent = 0.0
        self._has_prev_diff = False
        self._bars_from_signal = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(mh_hull_moving_average_based_trading_strategy, self).OnReseted()
        self._prev_diff_percent = 0.0
        self._has_prev_diff = False
        self._bars_from_signal = 0

    def OnStarted2(self, time):
        super(mh_hull_moving_average_based_trading_strategy, self).OnStarted2(time)
        self._prev_diff_percent = 0.0
        self._has_prev_diff = False
        self._bars_from_signal = self._signal_cooldown_bars.Value
        self._hma = HullMovingAverage()
        self._hma.Length = self._hull_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._hma, self.OnProcess).Start()

    def OnProcess(self, candle, hma_value):
        if candle.State != CandleStates.Finished:
            return
        if not self._hma.IsFormed:
            return
        price = float(candle.ClosePrice)
        if price <= 0.0:
            return
        hv = float(hma_value)
        diff_percent = (price - hv) / price * 100.0
        threshold = float(self._signal_threshold_percent.Value)
        crossed_up = self._has_prev_diff and self._prev_diff_percent <= threshold and diff_percent > threshold
        crossed_down = self._has_prev_diff and self._prev_diff_percent >= -threshold and diff_percent < -threshold
        self._prev_diff_percent = diff_percent
        self._has_prev_diff = True
        self._bars_from_signal += 1
        if self._bars_from_signal < self._signal_cooldown_bars.Value:
            return
        if crossed_up and self.Position <= 0:
            self.BuyMarket()
            self._bars_from_signal = 0
        elif crossed_down and self.Position >= 0:
            self.SellMarket()
            self._bars_from_signal = 0

    def CreateClone(self):
        return mh_hull_moving_average_based_trading_strategy()
