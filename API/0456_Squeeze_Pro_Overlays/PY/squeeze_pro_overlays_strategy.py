import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, KeltnerChannels, LinearRegSlope
from StockSharp.Algo.Strategies import Strategy


class squeeze_pro_overlays_strategy(Strategy):
    def __init__(self):
        super(squeeze_pro_overlays_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._squeeze_length = self.Param("SqueezeLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Squeeze Length", "Calculation length", "Squeeze")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk")
        self._was_squeezed = False
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value
    @property
    def squeeze_length(self):
        return self._squeeze_length.Value
    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    def OnReseted(self):
        super(squeeze_pro_overlays_strategy, self).OnReseted()
        self._was_squeezed = False
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(squeeze_pro_overlays_strategy, self).OnStarted(time)
        self._bb = BollingerBands()
        self._bb.Length = self.squeeze_length
        self._bb.Width = 2.0
        self._kc = KeltnerChannels()
        self._kc.Length = self.squeeze_length
        self._kc.Multiplier = 1.5
        self._slope = LinearRegSlope()
        self._slope.Length = self.squeeze_length

        subscription = self.SubscribeCandles(self.candle_type)
        subscription \
            .BindEx(self._bb, self._kc, self._slope, self.OnProcess) \
            .Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._bb)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, bb_value, kc_value, slope_value):
        if candle.State != CandleStates.Finished:
            return
        if not self._bb.IsFormed or not self._kc.IsFormed or not self._slope.IsFormed:
            return
        if bb_value.IsEmpty or kc_value.IsEmpty or slope_value.IsEmpty:
            return

        bb_upper = bb_value.UpBand
        bb_lower = bb_value.LowBand
        kc_upper = kc_value.Upper
        kc_lower = kc_value.Lower

        if bb_upper is None or bb_lower is None or kc_upper is None or kc_lower is None:
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        slope_val = float(slope_value)
        squeezed = float(bb_upper) < float(kc_upper) and float(bb_lower) > float(kc_lower)

        if self._was_squeezed and not squeezed:
            if slope_val > 0 and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket(abs(self.Position))
                self.BuyMarket()
                self._cooldown_remaining = self.cooldown_bars
            elif slope_val < 0 and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket(abs(self.Position))
                self.SellMarket()
                self._cooldown_remaining = self.cooldown_bars
        elif self.Position > 0 and slope_val < 0:
            self.SellMarket(abs(self.Position))
            self._cooldown_remaining = self.cooldown_bars
        elif self.Position < 0 and slope_val > 0:
            self.BuyMarket(abs(self.Position))
            self._cooldown_remaining = self.cooldown_bars

        self._was_squeezed = squeezed

    def CreateClone(self):
        return squeeze_pro_overlays_strategy()
