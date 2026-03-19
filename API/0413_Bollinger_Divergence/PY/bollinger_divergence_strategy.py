import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy


class bollinger_divergence_strategy(Strategy):
    def __init__(self):
        super(bollinger_divergence_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle type", "Candle type for strategy calculation.", "General")
        self._bb_length = self.Param("BBLength", 20) \
            .SetDisplay("BB Period", "Bollinger Bands period", "Bollinger Bands")
        self._bb_multiplier = self.Param("BBMultiplier", 2.0) \
            .SetDisplay("BB StdDev", "Bollinger Bands standard deviation multiplier", "Bollinger Bands")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk")
        self._prev_upper_band = 0.0
        self._prev_lower_band = 0.0
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
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    def OnReseted(self):
        super(bollinger_divergence_strategy, self).OnReseted()
        self._prev_upper_band = 0.0
        self._prev_lower_band = 0.0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(bollinger_divergence_strategy, self).OnStarted(time)
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
        if bb.UpBand is None or bb.LowBand is None or bb.MovingAverage is None:
            return
        upper_band = float(bb.UpBand)
        lower_band = float(bb.LowBand)
        middle_band = float(bb.MovingAverage)

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_upper_band = upper_band
            self._prev_lower_band = lower_band
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_upper_band = upper_band
            self._prev_lower_band = lower_band
            return

        close = float(candle.ClosePrice)

        if self._prev_upper_band > 0 and self._prev_lower_band > 0:
            bands_expanding = upper_band > self._prev_upper_band and lower_band < self._prev_lower_band
            bullish_candle = close > float(candle.OpenPrice)
            bearish_candle = close < float(candle.OpenPrice)

            if close > upper_band and bands_expanding and bullish_candle and self.Position <= 0:
                self.BuyMarket()
                self._cooldown_remaining = self.cooldown_bars
            elif close < lower_band and bands_expanding and bearish_candle and self.Position >= 0:
                self.SellMarket()
                self._cooldown_remaining = self.cooldown_bars
            elif self.Position > 0 and close < middle_band:
                self.SellMarket()
                self._cooldown_remaining = self.cooldown_bars
            elif self.Position < 0 and close > middle_band:
                self.BuyMarket()
                self._cooldown_remaining = self.cooldown_bars

        self._prev_upper_band = upper_band
        self._prev_lower_band = lower_band

    def CreateClone(self):
        return bollinger_divergence_strategy()
