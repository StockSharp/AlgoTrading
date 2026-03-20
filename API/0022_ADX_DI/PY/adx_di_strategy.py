import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageDirectionalIndex
from StockSharp.Algo.Strategies import Strategy


class adx_di_strategy(Strategy):

    def __init__(self):
        super(adx_di_strategy, self).__init__()

        self._adx_period = self.Param("AdxPeriod", 14) \
            .SetDisplay("ADX Period", "Period for ADX calculation", "Indicators")
        self._adx_threshold = self.Param("AdxThreshold", 15.0) \
            .SetDisplay("ADX Threshold", "ADX level to confirm trend", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_plus_di_above = False
        self._has_prev_values = False
        self._cooldown = 0

    @property
    def AdxPeriod(self):
        return self._adx_period.Value

    @AdxPeriod.setter
    def AdxPeriod(self, value):
        self._adx_period.Value = value

    @property
    def AdxThreshold(self):
        return self._adx_threshold.Value

    @AdxThreshold.setter
    def AdxThreshold(self, value):
        self._adx_threshold.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(adx_di_strategy, self).OnStarted(time)

        self._prev_plus_di_above = False
        self._has_prev_values = False
        self._cooldown = 0

        adx = AverageDirectionalIndex()
        adx.Length = self.AdxPeriod

        self.SubscribeCandles(self.CandleType) \
            .BindEx(adx, self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle, adx_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if adx_value.IsEmpty:
            return

        adx_main = adx_value.MovingAverage
        plus_di = adx_value.Dx.Plus
        minus_di = adx_value.Dx.Minus

        if adx_main is None or plus_di is None or minus_di is None:
            return

        adx_main_f = float(adx_main)
        plus_di_f = float(plus_di)
        minus_di_f = float(minus_di)

        plus_di_above = plus_di_f > minus_di_f

        if not self._has_prev_values:
            self._has_prev_values = True
            self._prev_plus_di_above = plus_di_above
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_plus_di_above = plus_di_above
            return

        threshold = float(self.AdxThreshold)

        if plus_di_above and not self._prev_plus_di_above and adx_main_f >= threshold and self.Position <= 0:
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)
            self._cooldown = 5
        elif not plus_di_above and self._prev_plus_di_above and adx_main_f >= threshold and self.Position >= 0:
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)
            self._cooldown = 5

        self._prev_plus_di_above = plus_di_above

    def OnReseted(self):
        super(adx_di_strategy, self).OnReseted()
        self._prev_plus_di_above = False
        self._has_prev_values = False
        self._cooldown = 0

    def CreateClone(self):
        return adx_di_strategy()
