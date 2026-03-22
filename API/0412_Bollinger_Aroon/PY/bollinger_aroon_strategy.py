import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, Aroon
from StockSharp.Algo.Strategies import Strategy


class bollinger_aroon_strategy(Strategy):
    """Bollinger Bands + Aroon Strategy.
    Buys when price touches lower BB with Aroon Up confirming uptrend.
    Exits when price reaches upper BB or Aroon signals weakness."""

    def __init__(self):
        super(bollinger_aroon_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle type", "Candle type for strategy calculation.", "General")
        self._bb_length = self.Param("BBLength", 20) \
            .SetDisplay("BB Period", "Bollinger Bands period", "Bollinger Bands")
        self._bb_multiplier = self.Param("BBMultiplier", 2.0) \
            .SetDisplay("BB StdDev", "Bollinger Bands standard deviation multiplier", "Bollinger Bands")
        self._aroon_length = self.Param("AroonLength", 14) \
            .SetDisplay("Aroon Period", "Aroon indicator period", "Aroon")
        self._aroon_confirmation = self.Param("AroonConfirmation", 60.0) \
            .SetDisplay("Aroon Confirmation", "Aroon confirmation level", "Aroon")
        self._aroon_stop = self.Param("AroonStop", 40.0) \
            .SetDisplay("Aroon Stop", "Aroon stop level", "Aroon")

        self._bollinger = None
        self._aroon = None
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(bollinger_aroon_strategy, self).OnReseted()
        self._bollinger = None
        self._aroon = None
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(bollinger_aroon_strategy, self).OnStarted(time)

        self._bollinger = BollingerBands()
        self._bollinger.Length = int(self._bb_length.Value)
        self._bollinger.Width = float(self._bb_multiplier.Value)

        self._aroon = Aroon()
        self._aroon.Length = int(self._aroon_length.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._bollinger, self._aroon, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._bollinger)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, bb_value, aroon_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._bollinger.IsFormed or not self._aroon.IsFormed:
            return

        if bb_value.UpBand is None or bb_value.LowBand is None or bb_value.MovingAverage is None:
            return
        if aroon_value.Up is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        lower_band = float(bb_value.LowBand)
        upper_band = float(bb_value.UpBand)
        aroon_up = float(aroon_value.Up)
        close = float(candle.ClosePrice)
        aroon_confirm = float(self._aroon_confirmation.Value)
        aroon_stop = float(self._aroon_stop.Value)

        if close <= lower_band and aroon_up > aroon_confirm and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = 12
        elif close >= upper_band and aroon_up < aroon_stop and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = 12
        elif self.Position > 0 and (close >= upper_band or aroon_up < aroon_stop):
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown_remaining = 12
        elif self.Position < 0 and (close <= lower_band or aroon_up > aroon_confirm):
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = 12

    def CreateClone(self):
        return bollinger_aroon_strategy()
