import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SmoothedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class amma_trend_strategy(Strategy):

    def __init__(self):
        super(amma_trend_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use for analysis", "General")
        self._ma_period = self.Param("MaPeriod", 25) \
            .SetDisplay("AMMA Period", "Period of the modified moving average", "Indicator")

        self._mma0 = None
        self._mma1 = None
        self._mma2 = None
        self._mma3 = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def MaPeriod(self):
        return self._ma_period.Value

    @MaPeriod.setter
    def MaPeriod(self, value):
        self._ma_period.Value = value

    def OnStarted(self, time):
        super(amma_trend_strategy, self).OnStarted(time)

        self._mma0 = None
        self._mma1 = None
        self._mma2 = None
        self._mma3 = None

        mma = SmoothedMovingAverage()
        mma.Length = self.MaPeriod

        self.SubscribeCandles(self.CandleType) \
            .Bind(mma, self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle, mma_val):
        if candle.State != CandleStates.Finished:
            return

        mma_f = float(mma_val)

        self._mma3 = self._mma2
        self._mma2 = self._mma1
        self._mma1 = self._mma0
        self._mma0 = mma_f

        if self._mma1 is None or self._mma2 is None or self._mma3 is None:
            return

        if self._mma2 < self._mma3 and self._mma1 > self._mma2 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif self._mma2 > self._mma3 and self._mma1 < self._mma2 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

    def OnReseted(self):
        super(amma_trend_strategy, self).OnReseted()
        self._mma0 = None
        self._mma1 = None
        self._mma2 = None
        self._mma3 = None

    def CreateClone(self):
        return amma_trend_strategy()
