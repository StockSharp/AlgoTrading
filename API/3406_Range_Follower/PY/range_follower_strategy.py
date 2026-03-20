import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class range_follower_strategy(Strategy):
    def __init__(self):
        super(range_follower_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15)))
        self._atr_period = self.Param("AtrPeriod", 14)
        self._range_period = self.Param("RangePeriod", 20)

        self._range_high = 0.0
        self._range_low = float('inf')
        self._bar_count = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def AtrPeriod(self):
        return self._atr_period.Value

    @AtrPeriod.setter
    def AtrPeriod(self, value):
        self._atr_period.Value = value

    @property
    def RangePeriod(self):
        return self._range_period.Value

    @RangePeriod.setter
    def RangePeriod(self, value):
        self._range_period.Value = value

    def OnReseted(self):
        super(range_follower_strategy, self).OnReseted()
        self._range_high = 0.0
        self._range_low = float('inf')
        self._bar_count = 0

    def OnStarted(self, time):
        super(range_follower_strategy, self).OnStarted(time)
        self._range_high = 0.0
        self._range_low = float('inf')
        self._bar_count = 0

        atr = AverageTrueRange()
        atr.Length = self.AtrPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(atr, self._process_candle).Start()

    def _process_candle(self, candle, atr_value):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        self._bar_count += 1

        if float(candle.HighPrice) > self._range_high:
            self._range_high = float(candle.HighPrice)
        if float(candle.LowPrice) < self._range_low:
            self._range_low = float(candle.LowPrice)

        if self._bar_count < self.RangePeriod:
            return

        threshold = float(atr_value) * 0.5

        if close > self._range_high - threshold and self.Position <= 0:
            self.BuyMarket()
            self._reset_range()
        elif close < self._range_low + threshold and self.Position >= 0:
            self.SellMarket()
            self._reset_range()

        if self._bar_count > self.RangePeriod * 2:
            self._reset_range()

    def _reset_range(self):
        self._range_high = 0.0
        self._range_low = float('inf')
        self._bar_count = 0

    def CreateClone(self):
        return range_follower_strategy()
