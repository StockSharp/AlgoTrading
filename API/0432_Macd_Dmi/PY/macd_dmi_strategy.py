import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergence, DirectionalIndex, IndicatorHelper
from StockSharp.Algo.Strategies import Strategy


class macd_dmi_strategy(Strategy):
    """MACD + DMI Strategy. MACD zero crossover with DMI directional confirmation."""

    def __init__(self):
        super(macd_dmi_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle type", "Candle type for strategy calculation.", "General")
        self._dmi_length = self.Param("DmiLength", 14) \
            .SetDisplay("DMI Length", "DMI period", "DMI")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk")

        self._macd = None
        self._dmi = None
        self._prev_macd = 0.0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(macd_dmi_strategy, self).OnReseted()
        self._macd = None
        self._dmi = None
        self._prev_macd = 0.0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(macd_dmi_strategy, self).OnStarted(time)

        self._macd = MovingAverageConvergenceDivergence()
        self._dmi = DirectionalIndex()
        self._dmi.Length = int(self._dmi_length.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._macd, self._dmi, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, macd_value, dmi_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._macd.IsFormed or not self._dmi.IsFormed:
            return

        if macd_value.IsEmpty:
            return

        macd_val = float(IndicatorHelper.ToDecimal(macd_value))

        if dmi_value.Plus is None or dmi_value.Minus is None:
            return

        di_plus = float(dmi_value.Plus)
        di_minus = float(dmi_value.Minus)

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_macd = macd_val
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_macd = macd_val
            return

        cooldown = int(self._cooldown_bars.Value)

        macd_cross_up = macd_val > 0 and self._prev_macd <= 0 and self._prev_macd != 0
        macd_cross_down = macd_val < 0 and self._prev_macd >= 0 and self._prev_macd != 0

        if macd_cross_up and di_plus > di_minus and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif macd_cross_down and di_minus > di_plus and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif self.Position > 0 and di_minus > di_plus and macd_val < 0:
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown
        elif self.Position < 0 and di_plus > di_minus and macd_val > 0:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown

        self._prev_macd = macd_val

    def CreateClone(self):
        return macd_dmi_strategy()
