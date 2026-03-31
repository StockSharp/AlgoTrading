import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

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

        self._atr = None
        self._highest = None
        self._trail_stop = 0.0
        self._entry_price = 0.0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(trend_following_stocks_strategy, self).OnReseted()
        self._atr = None
        self._highest = None
        self._trail_stop = 0.0
        self._entry_price = 0.0
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(trend_following_stocks_strategy, self).OnStarted2(time)
        self._atr = AverageTrueRange()
        self._atr.Length = int(self._atr_len.Value)
        self._highest = Highest()
        self._highest.Length = int(self._highest_len.Value)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._atr, self._highest, self._process_candle).Start()

    def _process_candle(self, candle, atr_val, highest_val):
        if candle.State != CandleStates.Finished:
            return

        if not self._atr.IsFormed or not self._highest.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        close = float(candle.ClosePrice)
        av = float(atr_val)
        hv = float(highest_val)
        atr_mult = float(self._atr_multiplier.Value)
        cooldown = int(self._cooldown_bars.Value)

        if self.Position > 0:
            candidate = close - av * atr_mult
            if candidate > self._trail_stop:
                self._trail_stop = candidate
            if close <= self._trail_stop:
                self.SellMarket(Math.Abs(self.Position))
                self._trail_stop = 0.0
                self._entry_price = 0.0
                self._cooldown_remaining = cooldown
        elif self._cooldown_remaining <= 0:
            if close >= hv:
                self.BuyMarket(self.Volume)
                self._entry_price = close
                self._trail_stop = close - av * atr_mult
                self._cooldown_remaining = cooldown

    def CreateClone(self):
        return trend_following_stocks_strategy()
