import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
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

        self._prev_di_plus = 0.0
        self._prev_di_minus = 0.0
        self._cooldown_remaining = 0

    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def DILength(self):
        return self._di_length.Value
    @property
    def ADXSmoothing(self):
        return self._adx_smoothing.Value
    @property
    def KeyLevel(self):
        return self._key_level.Value
    @property
    def MALength(self):
        return self._ma_length.Value
    @property
    def CooldownBars(self):
        return self._cooldown_bars.Value

    def OnReseted(self):
        super(dmi_winner_strategy, self).OnReseted()
        self._prev_di_plus = 0.0
        self._prev_di_minus = 0.0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(dmi_winner_strategy, self).OnStarted(time)

        dmi = DirectionalIndex()
        dmi.Length = self.DILength

        adx = AverageDirectionalIndex()
        adx.Length = self.ADXSmoothing

        ma = ExponentialMovingAverage()
        ma.Length = self.MALength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(dmi, adx, ma, self.OnProcess).Start()

    def OnProcess(self, candle, dmi_value, adx_value, ma_value):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
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

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_di_plus = di_plus
            self._prev_di_minus = di_minus
            return

        close = float(candle.ClosePrice)

        di_plus_cross_up = di_plus > di_minus and self._prev_di_plus <= self._prev_di_minus and self._prev_di_plus > 0
        di_plus_cross_down = di_plus < di_minus and self._prev_di_plus >= self._prev_di_minus and self._prev_di_plus > 0

        if di_plus_cross_up and adx_val > self.KeyLevel and close > ma_val and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self.CooldownBars
        elif di_plus_cross_down and adx_val > self.KeyLevel and close < ma_val and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = self.CooldownBars
        elif self.Position > 0 and di_plus_cross_down:
            self.SellMarket()
            self._cooldown_remaining = self.CooldownBars
        elif self.Position < 0 and di_plus_cross_up:
            self.BuyMarket()
            self._cooldown_remaining = self.CooldownBars

        self._prev_di_plus = di_plus
        self._prev_di_minus = di_minus

    def CreateClone(self):
        return dmi_winner_strategy()
