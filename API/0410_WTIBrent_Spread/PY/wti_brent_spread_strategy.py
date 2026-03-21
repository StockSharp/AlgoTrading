import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy


class wti_brent_spread_strategy(Strategy):
    """Mean-reversion spread strategy using Bollinger Bands."""

    def __init__(self):
        super(wti_brent_spread_strategy, self).__init__()

        self._bb_period = self.Param("BbPeriod", 20) \
            .SetDisplay("BB Period", "Bollinger Bands period", "Parameters")
        self._bb_width = self.Param("BbWidth", 2.0) \
            .SetDisplay("BB Width", "Bollinger Bands width in std devs", "Parameters")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._cooldown_remaining = 0

    @property
    def BbPeriod(self):
        return self._bb_period.Value
    @property
    def BbWidth(self):
        return self._bb_width.Value
    @property
    def CooldownBars(self):
        return self._cooldown_bars.Value
    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(wti_brent_spread_strategy, self).OnReseted()
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(wti_brent_spread_strategy, self).OnStarted(time)
        bb = BollingerBands()
        bb.Length = self.BbPeriod
        bb.Width = self.BbWidth
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(bb, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, bb_value):
        if candle.State != CandleStates.Finished:
            return
        if bb_value.UpBand is None or bb_value.LowBand is None or bb_value.MovingAverage is None:
            return

        upper = float(bb_value.UpBand)
        lower = float(bb_value.LowBand)
        middle = float(bb_value.MovingAverage)

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        close = float(candle.ClosePrice)

        if close <= lower and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self.CooldownBars
        elif close >= upper and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = self.CooldownBars
        elif self.Position > 0 and close >= middle:
            self.SellMarket()
            self._cooldown_remaining = self.CooldownBars
        elif self.Position < 0 and close <= middle:
            self.BuyMarket()
            self._cooldown_remaining = self.CooldownBars

    def CreateClone(self):
        return wti_brent_spread_strategy()
