import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class three_bar_low_strategy(Strategy):
    """3-Bar Low Strategy."""

    def __init__(self):
        super(three_bar_low_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._ema_length = self.Param("EmaLength", 50) \
            .SetDisplay("EMA Length", "EMA trend filter period", "Indicators")
        self._lookback_low = self.Param("LookbackLow", 3) \
            .SetDisplay("Lookback Low", "Bars for lowest low", "Parameters")
        self._lookback_high = self.Param("LookbackHigh", 7) \
            .SetDisplay("Lookback High", "Bars for highest high", "Parameters")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")

        self._ema = None
        self._lows = []
        self._highs = []
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(three_bar_low_strategy, self).OnReseted()
        self._ema = None
        self._lows = []
        self._highs = []
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(three_bar_low_strategy, self).OnStarted(time)

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

        lb_low = int(self._lookback_low.Value)
        lb_high = int(self._lookback_high.Value)

        self._lows.append(float(candle.LowPrice))
        self._highs.append(float(candle.HighPrice))

        if len(self._lows) > lb_low + 1:
            self._lows.pop(0)
        if len(self._highs) > lb_high + 1:
            self._highs.pop(0)

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        if len(self._lows) <= lb_low or len(self._highs) <= lb_high:
            return

        lowest_low = min(self._lows[:-1])
        highest_high = max(self._highs[:-1])
        price = float(candle.ClosePrice)
        cooldown = int(self._cooldown_bars.Value)

        if price < lowest_low and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif price > highest_high and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif self.Position > 0 and price > highest_high:
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown
        elif self.Position < 0 and price < lowest_low:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown

    def CreateClone(self):
        return three_bar_low_strategy()
