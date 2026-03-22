import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, KeltnerChannels, LinearRegSlope, IndicatorHelper
from StockSharp.Algo.Strategies import Strategy


class squeeze_pro_overlays_strategy(Strategy):
    """Squeeze Pro Overlays Strategy."""

    def __init__(self):
        super(squeeze_pro_overlays_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._squeeze_length = self.Param("SqueezeLength", 20) \
            .SetDisplay("Squeeze Length", "Calculation length", "Squeeze")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk")

        self._bb = None
        self._kc = None
        self._slope = None
        self._was_squeezed = False
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(squeeze_pro_overlays_strategy, self).OnReseted()
        self._bb = None
        self._kc = None
        self._slope = None
        self._was_squeezed = False
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(squeeze_pro_overlays_strategy, self).OnStarted(time)

        sq_len = int(self._squeeze_length.Value)

        self._bb = BollingerBands()
        self._bb.Length = sq_len
        self._bb.Width = 2.0

        self._kc = KeltnerChannels()
        self._kc.Length = sq_len
        self._kc.Multiplier = 1.5

        self._slope = LinearRegSlope()
        self._slope.Length = sq_len

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._bb, self._kc, self._slope, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._bb)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, bb_value, kc_value, slope_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._bb.IsFormed or not self._kc.IsFormed or not self._slope.IsFormed:
            return

        if bb_value.IsEmpty or kc_value.IsEmpty or slope_value.IsEmpty:
            return

        if bb_value.UpBand is None or bb_value.LowBand is None:
            return
        if kc_value.Upper is None or kc_value.Lower is None:
            return

        bb_upper = float(bb_value.UpBand)
        bb_lower = float(bb_value.LowBand)
        kc_upper = float(kc_value.Upper)
        kc_lower = float(kc_value.Lower)

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        slope_val = float(IndicatorHelper.ToDecimal(slope_value))
        squeezed = bb_upper < kc_upper and bb_lower > kc_lower
        cooldown = int(self._cooldown_bars.Value)

        if self._was_squeezed and not squeezed:
            if slope_val > 0 and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket(Math.Abs(self.Position))
                self.BuyMarket(self.Volume)
                self._cooldown_remaining = cooldown
            elif slope_val < 0 and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket(Math.Abs(self.Position))
                self.SellMarket(self.Volume)
                self._cooldown_remaining = cooldown
        elif self.Position > 0 and slope_val < 0:
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown
        elif self.Position < 0 and slope_val > 0:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown

        self._was_squeezed = squeezed

    def CreateClone(self):
        return squeeze_pro_overlays_strategy()
