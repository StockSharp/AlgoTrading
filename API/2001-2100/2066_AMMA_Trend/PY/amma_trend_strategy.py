import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, OrderStates
from StockSharp.Algo.Indicators import SmoothedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class amma_trend_strategy(Strategy):

    def __init__(self):
        super(amma_trend_strategy, self).__init__()

        self._allow_long_entry = self.Param("AllowLongEntry", True)
        self._allow_short_entry = self.Param("AllowShortEntry", True)
        self._allow_long_exit = self.Param("AllowLongExit", True)
        self._allow_short_exit = self.Param("AllowShortExit", True)
        self._pending = None
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

    def OnStarted2(self, time):
        super(amma_trend_strategy, self).OnStarted2(time)
        self._pending = None

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

        rising = self._mma2 < self._mma3 and self._mma1 > self._mma2 and self._mma0 > self._mma1
        falling = self._mma2 > self._mma3 and self._mma1 < self._mma2 and self._mma0 < self._mma1
        if not self.IsFormedAndOnlineAndAllowTrading() or (self._pending is not None and self._pending.State not in (OrderStates.Done, OrderStates.Failed)):
            return
        if rising:
            if self.Position < 0 and self._allow_short_exit.Value:
                self._pending = self.BuyMarket(-self.Position + (self.Volume if self._allow_long_entry.Value else 0))
            elif self.Position == 0 and self._allow_long_entry.Value:
                self._pending = self.BuyMarket(self.Volume)
        elif falling:
            if self.Position > 0 and self._allow_long_exit.Value:
                self._pending = self.SellMarket(self.Position + (self.Volume if self._allow_short_entry.Value else 0))
            elif self.Position == 0 and self._allow_short_entry.Value:
                self._pending = self.SellMarket(self.Volume)

    def OnReseted(self):
        super(amma_trend_strategy, self).OnReseted()
        self._pending = None
        self._mma0 = None
        self._mma1 = None
        self._mma2 = None
        self._mma3 = None

    def CreateClone(self):
        return amma_trend_strategy()
