import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy


class bollinger_winner_lite_strategy(Strategy):
    """Bollinger Bands Winner LITE Strategy. Buys when candle body extends below lower BB, sells when above upper BB."""

    def __init__(self):
        super(bollinger_winner_lite_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle type", "Candle type for strategy calculation.", "General")
        self._bb_length = self.Param("BBLength", 20) \
            .SetDisplay("BB Period", "Bollinger Bands period", "Bollinger Bands")
        self._bb_multiplier = self.Param("BBMultiplier", 1.5) \
            .SetDisplay("BB StdDev", "Bollinger Bands standard deviation multiplier", "Bollinger Bands")
        self._candle_percent = self.Param("CandlePercent", 30.0) \
            .SetDisplay("Candle %", "Candle percentage below/above the BB", "Strategy")
        self._show_short = self.Param("ShowShort", True) \
            .SetDisplay("Short entries", "Enable short entries", "Strategy")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk")

        self._bollinger = None
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(bollinger_winner_lite_strategy, self).OnReseted()
        self._bollinger = None
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(bollinger_winner_lite_strategy, self).OnStarted(time)
        self._bollinger = BollingerBands()
        self._bollinger.Length = int(self._bb_length.Value)
        self._bollinger.Width = float(self._bb_multiplier.Value)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._bollinger, self._on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._bollinger)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, bollinger_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._bollinger.IsFormed:
            return

        bb = bollinger_value
        if bb.UpBand is None or bb.LowBand is None:
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        upper_band = float(bb.UpBand)
        lower_band = float(bb.LowBand)
        close = float(candle.ClosePrice)
        cooldown = int(self._cooldown_bars.Value)

        buy = close <= lower_band
        sell = close >= upper_band

        if buy and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif sell:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
                self._cooldown_remaining = cooldown
            elif bool(self._show_short.Value) and self.Position == 0:
                self.SellMarket(self.Volume)
                self._cooldown_remaining = cooldown

    def CreateClone(self):
        return bollinger_winner_lite_strategy()
