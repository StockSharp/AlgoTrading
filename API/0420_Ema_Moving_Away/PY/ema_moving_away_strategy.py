import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class ema_moving_away_strategy(Strategy):
    """EMA Moving Away Strategy - mean reversion from EMA."""

    def __init__(self):
        super(ema_moving_away_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle type", "Candle type for strategy calculation.", "General")
        self._ema_length = self.Param("EmaLength", 55) \
            .SetDisplay("EMA Length", "EMA period", "Moving Average")
        self._moving_away_pct = self.Param("MovingAwayPercent", 1.5) \
            .SetDisplay("Moving away (%)", "Required percentage that price moves away from EMA", "Strategy")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk")

        self._cooldown_remaining = 0

    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def EmaLength(self):
        return self._ema_length.Value
    @property
    def MovingAwayPercent(self):
        return self._moving_away_pct.Value
    @property
    def CooldownBars(self):
        return self._cooldown_bars.Value

    def OnReseted(self):
        super(ema_moving_away_strategy, self).OnReseted()
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(ema_moving_away_strategy, self).OnStarted(time)
        ema = ExponentialMovingAverage()
        ema.Length = self.EmaLength
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ema, self.OnProcess).Start()

    def OnProcess(self, candle, ema_val):
        if candle.State != CandleStates.Finished:
            return
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        close = float(candle.ClosePrice)
        ev = float(ema_val)

        long_entry = ev * (1.0 - self.MovingAwayPercent / 100.0)
        short_entry = ev * (1.0 + self.MovingAwayPercent / 100.0)

        if self.Position > 0 and close >= ev:
            self.SellMarket()
            self._cooldown_remaining = self.CooldownBars
            return
        elif self.Position < 0 and close <= ev:
            self.BuyMarket()
            self._cooldown_remaining = self.CooldownBars
            return

        if close <= long_entry and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self.CooldownBars
        elif close >= short_entry and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = self.CooldownBars

    def CreateClone(self):
        return ema_moving_away_strategy()
