import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WilliamsR
from StockSharp.Algo.Strategies import Strategy

class wpr_custom_cloud_simple_strategy(Strategy):
    def __init__(self):
        super(wpr_custom_cloud_simple_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._wpr_period = self.Param("WprPeriod", 20)
        self._overbought_level = self.Param("OverboughtLevel", -10.0)
        self._oversold_level = self.Param("OversoldLevel", -90.0)

        self._prev_wpr = 0.0
        self._older_wpr = 0.0
        self._count = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def WprPeriod(self):
        return self._wpr_period.Value

    @WprPeriod.setter
    def WprPeriod(self, value):
        self._wpr_period.Value = value

    @property
    def OverboughtLevel(self):
        return self._overbought_level.Value

    @OverboughtLevel.setter
    def OverboughtLevel(self, value):
        self._overbought_level.Value = value

    @property
    def OversoldLevel(self):
        return self._oversold_level.Value

    @OversoldLevel.setter
    def OversoldLevel(self, value):
        self._oversold_level.Value = value

    def OnReseted(self):
        super(wpr_custom_cloud_simple_strategy, self).OnReseted()
        self._prev_wpr = 0.0
        self._older_wpr = 0.0
        self._count = 0

    def OnStarted(self, time):
        super(wpr_custom_cloud_simple_strategy, self).OnStarted(time)
        self._prev_wpr = 0.0
        self._older_wpr = 0.0
        self._count = 0

        wpr = WilliamsR()
        wpr.Length = self.WprPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(wpr, self._process_candle).Start()

    def _process_candle(self, candle, wpr_value):
        if candle.State != CandleStates.Finished:
            return

        wpr_val = float(wpr_value)
        self._count += 1

        if self._count >= 3:
            oversold = float(self.OversoldLevel)
            overbought = float(self.OverboughtLevel)

            crossed_above_oversold = self._older_wpr < oversold and self._prev_wpr > oversold
            crossed_below_overbought = self._older_wpr > overbought and self._prev_wpr < overbought

            if crossed_above_oversold and self.Position <= 0:
                self.BuyMarket()
            elif crossed_below_overbought and self.Position >= 0:
                self.SellMarket()

        self._older_wpr = self._prev_wpr
        self._prev_wpr = wpr_val

    def CreateClone(self):
        return wpr_custom_cloud_simple_strategy()
