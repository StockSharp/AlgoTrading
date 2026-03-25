import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, WilliamsR
from StockSharp.Algo.Strategies import Strategy


class weight_oscillator_strategy(Strategy):
    def __init__(self):
        super(weight_oscillator_strategy, self).__init__()

        self._rsi_period = self.Param("RsiPeriod", 14)
        self._wpr_period = self.Param("WprPeriod", 14)
        self._high_level = self.Param("HighLevel", 70.0)
        self._low_level = self.Param("LowLevel", 30.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30)))

        self._prev_osc = 0.0
        self._has_prev = False

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsi_period.Value = value

    @property
    def WprPeriod(self):
        return self._wpr_period.Value

    @WprPeriod.setter
    def WprPeriod(self, value):
        self._wpr_period.Value = value

    @property
    def HighLevel(self):
        return self._high_level.Value

    @HighLevel.setter
    def HighLevel(self, value):
        self._high_level.Value = value

    @property
    def LowLevel(self):
        return self._low_level.Value

    @LowLevel.setter
    def LowLevel(self, value):
        self._low_level.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(weight_oscillator_strategy, self).OnStarted(time)

        self._has_prev = False
        self._prev_osc = 0.0

        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod
        wpr = WilliamsR()
        wpr.Length = self.WprPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, wpr, self.ProcessCandle).Start()


    def ProcessCandle(self, candle, rsi_value, wpr_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        rsi_val = float(rsi_value)
        wpr_val = float(wpr_value)

        normalized_wpr = wpr_val + 100.0
        osc = (rsi_val + normalized_wpr) / 2.0

        high_lvl = float(self.HighLevel)
        low_lvl = float(self.LowLevel)

        if self._has_prev:
            if self._prev_osc > low_lvl and osc <= low_lvl and self.Position <= 0:
                volume = float(self.Volume) + abs(float(self.Position))
                self.BuyMarket(volume)
            elif self._prev_osc < high_lvl and osc >= high_lvl and self.Position >= 0:
                volume = float(self.Volume) + abs(float(self.Position))
                self.SellMarket(volume)

        self._prev_osc = osc
        self._has_prev = True

    def OnReseted(self):
        super(weight_oscillator_strategy, self).OnReseted()
        self._prev_osc = 0.0
        self._has_prev = False

    def CreateClone(self):
        return weight_oscillator_strategy()
