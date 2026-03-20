import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class merovinh_mean_reversion_lowest_low_strategy(Strategy):
    def __init__(self):
        super(merovinh_mean_reversion_lowest_low_strategy, self).__init__()
        self._bars = self.Param("Bars", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Bars", "Lookback for highest/lowest", "General")
        self._breakout_percent = self.Param("BreakoutPercent", 0.4) \
            .SetGreaterThanZero() \
            .SetDisplay("Breakout Percent", "Minimum percentage change for new high/low", "General")
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 12) \
            .SetGreaterThanZero() \
            .SetDisplay("Signal Cooldown Bars", "Minimum bars between entries", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Candles timeframe", "General")
        self._prev_low = 0.0
        self._prev_high = 0.0
        self._bars_from_signal = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(merovinh_mean_reversion_lowest_low_strategy, self).OnReseted()
        self._prev_low = 0.0
        self._prev_high = 0.0
        self._bars_from_signal = 0

    def OnStarted(self, time):
        super(merovinh_mean_reversion_lowest_low_strategy, self).OnStarted(time)
        self._prev_low = 0.0
        self._prev_high = 0.0
        self._bars_from_signal = self._signal_cooldown_bars.Value
        self._highest = Highest()
        self._highest.Length = self._bars.Value
        self._lowest = Lowest()
        self._lowest.Length = self._bars.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._highest, self._lowest, self.OnProcess).Start()

    def OnProcess(self, candle, highest_high, lowest_low):
        if candle.State != CandleStates.Finished:
            return
        hh = float(highest_high)
        ll = float(lowest_low)
        if not self._highest.IsFormed or not self._lowest.IsFormed:
            self._prev_low = ll
            self._prev_high = hh
            return
        self._bars_from_signal += 1
        bp = float(self._breakout_percent.Value) / 100.0
        low_break = self._prev_low > 0.0 and ll < self._prev_low * (1.0 - bp)
        high_break = self._prev_high > 0.0 and hh > self._prev_high * (1.0 + bp)
        cd = self._signal_cooldown_bars.Value
        if self._bars_from_signal >= cd and low_break and self.Position == 0:
            self.BuyMarket()
            self._bars_from_signal = 0
        if high_break and self.Position > 0:
            self.SellMarket()
        self._prev_low = ll
        self._prev_high = hh

    def CreateClone(self):
        return merovinh_mean_reversion_lowest_low_strategy()
