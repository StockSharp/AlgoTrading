import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
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

        self._ema = None
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ema_moving_away_strategy, self).OnReseted()
        self._ema = None
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(ema_moving_away_strategy, self).OnStarted2(time)

        self._ema = ExponentialMovingAverage()
        self._ema.Length = int(self._ema_length.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._ema, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ema)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, ema_val):
        if candle.State != CandleStates.Finished:
            return

        if not self._ema.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        close = float(candle.ClosePrice)
        ev = float(ema_val)
        pct = float(self._moving_away_pct.Value)
        cooldown = int(self._cooldown_bars.Value)

        long_entry = ev * (1.0 - pct / 100.0)
        short_entry = ev * (1.0 + pct / 100.0)

        if self.Position > 0 and close >= ev:
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown
            return
        elif self.Position < 0 and close <= ev:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown
            return

        if close <= long_entry and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif close >= short_entry and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = cooldown

    def CreateClone(self):
        return ema_moving_away_strategy()
