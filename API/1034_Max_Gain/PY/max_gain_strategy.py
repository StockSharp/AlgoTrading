import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class max_gain_strategy(Strategy):
    def __init__(self):
        super(max_gain_strategy, self).__init__()
        self._period_length = self.Param("PeriodLength", 64) \
            .SetGreaterThanZero() \
            .SetDisplay("Period Length", "Rolling high-low length", "General")
        self._edge_multiplier = self.Param("EdgeMultiplier", 1.25) \
            .SetGreaterThanZero() \
            .SetDisplay("Edge Multiplier", "Upside/downside ratio threshold", "General")
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("Signal Cooldown Bars", "Minimum bars between entries", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Candles timeframe", "General")
        self._bars_from_signal = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(max_gain_strategy, self).OnReseted()
        self._bars_from_signal = 0

    def OnStarted2(self, time):
        super(max_gain_strategy, self).OnStarted2(time)
        self._bars_from_signal = self._signal_cooldown_bars.Value
        self._highest = Highest()
        self._highest.Length = self._period_length.Value
        self._lowest = Lowest()
        self._lowest.Length = self._period_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._highest, self._lowest, self.OnProcess).Start()

    def OnProcess(self, candle, max_high, min_low):
        if candle.State != CandleStates.Finished:
            return
        if not self._highest.IsFormed or not self._lowest.IsFormed:
            return
        mh = float(max_high)
        ml = float(min_low)
        close = float(candle.ClosePrice)
        if close <= 0.0 or mh <= ml:
            return
        self._bars_from_signal += 1
        cd = self._signal_cooldown_bars.Value
        if self._bars_from_signal < cd:
            return
        upside = (mh - close) / close
        downside = (close - ml) / close
        em = float(self._edge_multiplier.Value)
        if upside > downside * em and self.Position <= 0:
            self.BuyMarket()
            self._bars_from_signal = 0
        elif downside > upside * em and self.Position >= 0:
            self.SellMarket()
            self._bars_from_signal = 0

    def CreateClone(self):
        return max_gain_strategy()
