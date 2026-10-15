import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import LinearReg
from StockSharp.Algo.Strategies import Strategy


class vinin_i_trend_lrma_strategy(Strategy):

    def __init__(self):
        super(vinin_i_trend_lrma_strategy, self).__init__()

        self._period = self.Param("Period", 13) \
            .SetDisplay("LRMA period", "Linear regression period", "General")
        self._up_level = self.Param("UpLevel", 0.1) \
            .SetDisplay("Upper level", "Upper trigger level (percent)", "General")
        self._dn_level = self.Param("DnLevel", -0.1) \
            .SetDisplay("Lower level", "Lower trigger level (percent)", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle Type", "General")

        self._prev_osc = None

    @property
    def Period(self):
        return self._period.Value

    @Period.setter
    def Period(self, value):
        self._period.Value = value

    @property
    def UpLevel(self):
        return self._up_level.Value

    @UpLevel.setter
    def UpLevel(self, value):
        self._up_level.Value = value

    @property
    def DnLevel(self):
        return self._dn_level.Value

    @DnLevel.setter
    def DnLevel(self, value):
        self._dn_level.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(vinin_i_trend_lrma_strategy, self).OnStarted2(time)

        self._prev_osc = None

        lrma = LinearReg()
        lrma.Length = self.Period

        self.SubscribeCandles(self.CandleType) \
            .Bind(lrma, self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle, lrma_value):
        if candle.State != CandleStates.Finished:
            return

        lrma = float(lrma_value)
        if lrma == 0:
            return

        osc = (float(candle.ClosePrice) - lrma) / lrma * 100.0

        if self._prev_osc is not None:
            up = float(self.UpLevel)
            dn = float(self.DnLevel)

            if osc > up and self._prev_osc <= up and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
            elif osc < dn and self._prev_osc >= dn and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()

        self._prev_osc = osc

    def OnReseted(self):
        super(vinin_i_trend_lrma_strategy, self).OnReseted()
        self._prev_osc = None

    def CreateClone(self):
        return vinin_i_trend_lrma_strategy()
