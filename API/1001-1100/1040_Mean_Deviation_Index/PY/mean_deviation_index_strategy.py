import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class mean_deviation_index_strategy(Strategy):
    def __init__(self):
        super(mean_deviation_index_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 26) \
            .SetGreaterThanZero() \
            .SetDisplay("EMA Period", "EMA length", "General")
        self._atr_period = self.Param("AtrPeriod", 26) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "ATR length", "General")
        self._atr_multiplier = self.Param("AtrMultiplier", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Multiplier", "ATR scaling", "General")
        self._level = self.Param("Level", 1.2) \
            .SetGreaterThanZero() \
            .SetDisplay("Level", "Normalized MDI threshold", "General")
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Signal Cooldown Bars", "Minimum bars between entries", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Candles timeframe", "General")
        self._previous_mdx = 0.0
        self._has_previous_mdx = False
        self._bars_from_signal = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(mean_deviation_index_strategy, self).OnReseted()
        self._previous_mdx = 0.0
        self._has_previous_mdx = False
        self._bars_from_signal = 0

    def OnStarted2(self, time):
        super(mean_deviation_index_strategy, self).OnStarted2(time)
        self._previous_mdx = 0.0
        self._has_previous_mdx = False
        self._bars_from_signal = self._signal_cooldown_bars.Value
        self._ema = ExponentialMovingAverage()
        self._ema.Length = self._ema_period.Value
        self._atr = AverageTrueRange()
        self._atr.Length = self._atr_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._ema, self._atr, self.OnProcess).Start()

    def OnProcess(self, candle, ema_value, atr_value):
        if candle.State != CandleStates.Finished:
            return
        if not self._ema.IsFormed or not self._atr.IsFormed:
            return
        ev = float(ema_value)
        av = float(atr_value) * float(self._atr_multiplier.Value)
        if av <= 0.0:
            return
        close = float(candle.ClosePrice)
        dev = close - ev
        mdx = dev / av
        lv = float(self._level.Value)
        crossed_up = self._has_previous_mdx and self._previous_mdx <= lv and mdx > lv
        crossed_down = self._has_previous_mdx and self._previous_mdx >= -lv and mdx < -lv
        self._previous_mdx = mdx
        self._has_previous_mdx = True
        self._bars_from_signal += 1
        cd = self._signal_cooldown_bars.Value
        if self._bars_from_signal < cd:
            return
        if crossed_up and self.Position <= 0:
            self.BuyMarket()
            self._bars_from_signal = 0
        elif crossed_down and self.Position >= 0:
            self.SellMarket()
            self._bars_from_signal = 0

    def CreateClone(self):
        return mean_deviation_index_strategy()
