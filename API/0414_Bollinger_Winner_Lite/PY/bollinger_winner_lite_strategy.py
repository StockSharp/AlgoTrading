import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy


class bollinger_winner_lite_strategy(Strategy):
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
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value
    @property
    def bb_length(self):
        return self._bb_length.Value
    @property
    def bb_multiplier(self):
        return self._bb_multiplier.Value
    @property
    def candle_percent(self):
        return self._candle_percent.Value
    @property
    def show_short(self):
        return self._show_short.Value
    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    def OnReseted(self):
        super(bollinger_winner_lite_strategy, self).OnReseted()
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(bollinger_winner_lite_strategy, self).OnStarted(time)
        bollinger = BollingerBands()
        bollinger.Length = self.bb_length
        bollinger.Width = self.bb_multiplier
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bollinger, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, bollinger_value):
        if candle.State != CandleStates.Finished:
            return
        bb = bollinger_value
        if bb.UpBand is None or bb.LowBand is None:
            return
        upper_band = float(bb.UpBand)
        lower_band = float(bb.LowBand)

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        close = float(candle.ClosePrice)
        buy = close <= lower_band
        sell = close >= upper_band

        if buy and self.Position <= 0:
            self.BuyMarket()
            self._cooldown_remaining = self.cooldown_bars
        elif sell:
            if self.Position > 0:
                self.SellMarket()
                self._cooldown_remaining = self.cooldown_bars
            elif self.show_short and self.Position == 0:
                self.SellMarket()
                self._cooldown_remaining = self.cooldown_bars

    def CreateClone(self):
        return bollinger_winner_lite_strategy()
