import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
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

        self._bb = None
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(wti_brent_spread_strategy, self).OnReseted()
        self._bb = None
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(wti_brent_spread_strategy, self).OnStarted2(time)
        self._bb = BollingerBands()
        self._bb.Length = int(self._bb_period.Value)
        self._bb.Width = float(self._bb_width.Value)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._bb, self._process_candle).Start()

    def _process_candle(self, candle, bb_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._bb.IsFormed:
            return

        if bb_value.UpBand is None or bb_value.LowBand is None or bb_value.MovingAverage is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        upper = float(bb_value.UpBand)
        lower = float(bb_value.LowBand)
        middle = float(bb_value.MovingAverage)
        close = float(candle.ClosePrice)
        cooldown = int(self._cooldown_bars.Value)

        if close <= lower and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif close >= upper and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif self.Position > 0 and close >= middle:
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown
        elif self.Position < 0 and close <= middle:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown

    def CreateClone(self):
        return wti_brent_spread_strategy()
