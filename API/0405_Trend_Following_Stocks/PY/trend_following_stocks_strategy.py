import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, Highest
from StockSharp.Algo.Strategies import Strategy


class trend_following_stocks_strategy(Strategy):
    """Breakout trend-following strategy with ATR trailing stop."""

    def __init__(self):
        super(trend_following_stocks_strategy, self).__init__()

        self._atr_len = self.Param("AtrLen", 14) \
            .SetDisplay("ATR Length", "ATR period length", "Parameters")
        self._highest_len = self.Param("HighestLen", 40) \
            .SetDisplay("Highest Length", "Lookback period for highest high", "Parameters")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.5) \
            .SetDisplay("ATR Multiplier", "ATR multiplier for trailing stop", "Parameters")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._trail_stop = 0.0
        self._entry_price = 0.0
        self._cooldown_remaining = 0

    @property
    def AtrLen(self):
        return self._atr_len.Value
    @property
    def HighestLen(self):
        return self._highest_len.Value
    @property
    def AtrMultiplier(self):
        return self._atr_multiplier.Value
    @property
    def CooldownBars(self):
        return self._cooldown_bars.Value
    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(trend_following_stocks_strategy, self).OnReseted()
        self._trail_stop = 0.0
        self._entry_price = 0.0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(trend_following_stocks_strategy, self).OnStarted(time)
        atr = AverageTrueRange()
        atr.Length = self.AtrLen
        highest = Highest()
        highest.Length = self.HighestLen
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(atr, highest, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, atr_val, highest_val):
        if candle.State != CandleStates.Finished:
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        close = float(candle.ClosePrice)
        av = float(atr_val)
        hv = float(highest_val)

        if self.Position > 0:
            candidate = close - av * self.AtrMultiplier
            if candidate > self._trail_stop:
                self._trail_stop = candidate
            if close <= self._trail_stop:
                self.SellMarket()
                self._trail_stop = 0.0
                self._entry_price = 0.0
                self._cooldown_remaining = self.CooldownBars
        elif self._cooldown_remaining <= 0:
            if close >= hv:
                self.BuyMarket()
                self._entry_price = close
                self._trail_stop = close - av * self.AtrMultiplier
                self._cooldown_remaining = self.CooldownBars

    def CreateClone(self):
        return trend_following_stocks_strategy()
