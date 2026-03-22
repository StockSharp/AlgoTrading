import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import DirectionalIndex, AverageDirectionalIndex, ExponentialMovingAverage, IndicatorHelper
from StockSharp.Algo.Strategies import Strategy


class dmi_winner_strategy(Strategy):
    """Directional Movement Index Winner Strategy.
    Uses DMI crossover with ADX confirmation and EMA trend filter."""

    def __init__(self):
        super(dmi_winner_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle type", "Candle type for strategy calculation.", "General")
        self._di_length = self.Param("DILength", 14) \
            .SetDisplay("DI Length", "Directional Indicator period", "DMI")
        self._adx_smoothing = self.Param("ADXSmoothing", 13) \
            .SetDisplay("ADX Smoothing", "ADX smoothing period", "DMI")
        self._key_level = self.Param("KeyLevel", 20.0) \
            .SetDisplay("Key Level", "ADX key level threshold", "DMI")
        self._ma_length = self.Param("MALength", 50) \
            .SetDisplay("MA Length", "Moving average period", "Moving Average")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk")

        self._dmi = None
        self._adx = None
        self._ma = None
        self._prev_di_plus = 0.0
        self._prev_di_minus = 0.0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(dmi_winner_strategy, self).OnReseted()
        self._dmi = None
        self._adx = None
        self._ma = None
        self._prev_di_plus = 0.0
        self._prev_di_minus = 0.0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(dmi_winner_strategy, self).OnStarted(time)

        self._dmi = DirectionalIndex()
        self._dmi.Length = int(self._di_length.Value)

        self._adx = AverageDirectionalIndex()
        self._adx.Length = int(self._adx_smoothing.Value)

        self._ma = ExponentialMovingAverage()
        self._ma.Length = int(self._ma_length.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._dmi, self._adx, self._ma, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ma)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, dmi_value, adx_value, ma_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._dmi.IsFormed or not self._adx.IsFormed or not self._ma.IsFormed:
            return

        if dmi_value.Plus is None or dmi_value.Minus is None:
            return
        if adx_value.MovingAverage is None:
            return
        if ma_value.IsEmpty:
            return

        di_plus = float(dmi_value.Plus)
        di_minus = float(dmi_value.Minus)
        adx_val = float(adx_value.MovingAverage)
        ma_val = float(IndicatorHelper.ToDecimal(ma_value))

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_di_plus = di_plus
            self._prev_di_minus = di_minus
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_di_plus = di_plus
            self._prev_di_minus = di_minus
            return

        close = float(candle.ClosePrice)
        cooldown = int(self._cooldown_bars.Value)
        key_level = float(self._key_level.Value)

        di_plus_cross_up = di_plus > di_minus and self._prev_di_plus <= self._prev_di_minus and self._prev_di_plus > 0
        di_plus_cross_down = di_plus < di_minus and self._prev_di_plus >= self._prev_di_minus and self._prev_di_plus > 0

        if di_plus_cross_up and adx_val > key_level and close > ma_val and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif di_plus_cross_down and adx_val > key_level and close < ma_val and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif self.Position > 0 and di_plus_cross_down:
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown
        elif self.Position < 0 and di_plus_cross_up:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown

        self._prev_di_plus = di_plus
        self._prev_di_minus = di_minus

    def CreateClone(self):
        return dmi_winner_strategy()
