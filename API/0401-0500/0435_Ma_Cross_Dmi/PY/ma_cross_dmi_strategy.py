import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, DirectionalIndex, IndicatorHelper
from StockSharp.Algo.Strategies import Strategy


class ma_cross_dmi_strategy(Strategy):
    """MA Cross + DMI Strategy. MA crossover confirmed by DMI direction."""

    def __init__(self):
        super(ma_cross_dmi_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle type", "Candle type for strategy calculation.", "General")
        self._ma1_length = self.Param("Ma1Length", 10) \
            .SetDisplay("MA1 Length", "Fast moving average period", "Moving Average")
        self._ma2_length = self.Param("Ma2Length", 20) \
            .SetDisplay("MA2 Length", "Slow moving average period", "Moving Average")
        self._dmi_length = self.Param("DmiLength", 14) \
            .SetDisplay("DMI Length", "DMI period", "DMI")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk")

        self._ma1 = None
        self._ma2 = None
        self._dmi = None
        self._prev_ma1 = 0.0
        self._prev_ma2 = 0.0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ma_cross_dmi_strategy, self).OnReseted()
        self._ma1 = None
        self._ma2 = None
        self._dmi = None
        self._prev_ma1 = 0.0
        self._prev_ma2 = 0.0
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(ma_cross_dmi_strategy, self).OnStarted2(time)

        self._ma1 = ExponentialMovingAverage()
        self._ma1.Length = int(self._ma1_length.Value)

        self._ma2 = ExponentialMovingAverage()
        self._ma2.Length = int(self._ma2_length.Value)

        self._dmi = DirectionalIndex()
        self._dmi.Length = int(self._dmi_length.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._ma1, self._ma2, self._dmi, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ma1)
            self.DrawIndicator(area, self._ma2)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, ma1_value, ma2_value, dmi_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._ma1.IsFormed or not self._ma2.IsFormed or not self._dmi.IsFormed:
            return

        if ma1_value.IsEmpty or ma2_value.IsEmpty or dmi_value.IsEmpty:
            return

        ma1 = float(IndicatorHelper.ToDecimal(ma1_value))
        ma2 = float(IndicatorHelper.ToDecimal(ma2_value))

        if dmi_value.Plus is None or dmi_value.Minus is None:
            self._prev_ma1 = ma1
            self._prev_ma2 = ma2
            return

        di_plus = float(dmi_value.Plus)
        di_minus = float(dmi_value.Minus)

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_ma1 = ma1
            self._prev_ma2 = ma2
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_ma1 = ma1
            self._prev_ma2 = ma2
            return

        if self._prev_ma1 == 0.0 or self._prev_ma2 == 0.0:
            self._prev_ma1 = ma1
            self._prev_ma2 = ma2
            return

        cooldown = int(self._cooldown_bars.Value)
        ma_cross_up = ma1 > ma2 and self._prev_ma1 <= self._prev_ma2
        ma_cross_down = ma1 < ma2 and self._prev_ma1 >= self._prev_ma2

        if ma_cross_up and di_plus > di_minus and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif ma_cross_down and di_minus > di_plus and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif self.Position > 0 and ma_cross_down:
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown
        elif self.Position < 0 and ma_cross_up:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown

        self._prev_ma1 = ma1
        self._prev_ma2 = ma2

    def CreateClone(self):
        return ma_cross_dmi_strategy()
