import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, IndicatorHelper
from StockSharp.Algo.Strategies import Strategy


class mtf_bb_strategy(Strategy):
    """Multi-Timeframe Bollinger Bands Strategy."""

    def __init__(self):
        super(mtf_bb_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle type", "Candle type for strategy calculation.", "General")
        self._bb_short_length = self.Param("BbShortLength", 20) \
            .SetDisplay("BB Short Length", "Short-period Bollinger Bands", "Bollinger Bands")
        self._bb_long_length = self.Param("BbLongLength", 50) \
            .SetDisplay("BB Long Length", "Long-period Bollinger Bands (MTF proxy)", "Bollinger Bands")
        self._bb_multiplier = self.Param("BBMultiplier", 2.0) \
            .SetDisplay("BB StdDev", "Standard deviation multiplier", "Bollinger Bands")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk")

        self._bb_short = None
        self._bb_long = None
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(mtf_bb_strategy, self).OnReseted()
        self._bb_short = None
        self._bb_long = None
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(mtf_bb_strategy, self).OnStarted2(time)

        self._bb_short = BollingerBands()
        self._bb_short.Length = int(self._bb_short_length.Value)
        self._bb_short.Width = float(self._bb_multiplier.Value)

        self._bb_long = BollingerBands()
        self._bb_long.Length = int(self._bb_long_length.Value)
        self._bb_long.Width = float(self._bb_multiplier.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._bb_short, self._bb_long, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._bb_short)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, bb_short_value, bb_long_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._bb_short.IsFormed or not self._bb_long.IsFormed:
            return

        if bb_short_value.IsEmpty or bb_long_value.IsEmpty:
            return

        if bb_short_value.UpBand is None or bb_short_value.LowBand is None:
            return
        if bb_long_value.UpBand is None or bb_long_value.LowBand is None:
            return

        short_upper = float(bb_short_value.UpBand)
        short_lower = float(bb_short_value.LowBand)
        long_upper = float(bb_long_value.UpBand)
        long_lower = float(bb_long_value.LowBand)

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        close = float(candle.ClosePrice)
        cooldown = int(self._cooldown_bars.Value)

        if close <= long_lower and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif close >= long_upper and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif self.Position > 0 and close >= short_upper:
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown
        elif self.Position < 0 and close <= short_lower:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown

    def CreateClone(self):
        return mtf_bb_strategy()
