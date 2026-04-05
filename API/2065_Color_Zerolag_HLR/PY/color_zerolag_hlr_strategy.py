import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest, CandleIndicatorValue
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

class color_zerolag_hlr_strategy(Strategy):
    """
    Strategy based on weighted Hi-Lo Range oscillator with zero lag smoothing.
    """

    def __init__(self):
        super(color_zerolag_hlr_strategy, self).__init__()
        self._smoothing = self.Param("Smoothing", 15) \
            .SetDisplay("Smoothing", "EMA smoothing factor", "Indicator")
        self._factor1 = self.Param("Factor1", 0.2) \
            .SetDisplay("Factor 1", "Weight for HLR period 1", "Indicator")
        self._hlr_period1 = self.Param("HlrPeriod1", 8) \
            .SetDisplay("HLR Period 1", "Lookback for HLR 1", "Indicator")
        self._factor2 = self.Param("Factor2", 0.35) \
            .SetDisplay("Factor 2", "Weight for HLR period 2", "Indicator")
        self._hlr_period2 = self.Param("HlrPeriod2", 21) \
            .SetDisplay("HLR Period 2", "Lookback for HLR 2", "Indicator")
        self._factor3 = self.Param("Factor3", 0.45) \
            .SetDisplay("Factor 3", "Weight for HLR period 3", "Indicator")
        self._hlr_period3 = self.Param("HlrPeriod3", 34) \
            .SetDisplay("HLR Period 3", "Lookback for HLR 3", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe for calculations", "General")

        self._smooth_const = 0.0
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._is_first = True

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(color_zerolag_hlr_strategy, self).OnReseted()
        self._is_first = True
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._smooth_const = 0.0

    def OnStarted2(self, time):
        super(color_zerolag_hlr_strategy, self).OnStarted2(time)

        smoothing = self._smoothing.Value
        self._smooth_const = (smoothing - 1.0) / smoothing
        self._is_first = True

        self._high1 = Highest()
        self._high1.Length = self._hlr_period1.Value
        self._low1 = Lowest()
        self._low1.Length = self._hlr_period1.Value
        self._high2 = Highest()
        self._high2.Length = self._hlr_period2.Value
        self._low2 = Lowest()
        self._low2.Length = self._hlr_period2.Value
        self._high3 = Highest()
        self._high3.Length = self._hlr_period3.Value
        self._low3 = Lowest()
        self._low3.Length = self._hlr_period3.Value

        self.Indicators.Add(self._high1)
        self.Indicators.Add(self._low1)
        self.Indicators.Add(self._high2)
        self.Indicators.Add(self._low2)
        self.Indicators.Add(self._high3)
        self.Indicators.Add(self._low3)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.on_process).Start()

    def on_process(self, candle):
        if candle.State != CandleStates.Finished:
            return

        cv1 = CandleIndicatorValue(self._high1, candle)
        h1 = self._high1.Process(cv1)
        cv2 = CandleIndicatorValue(self._low1, candle)
        l1 = self._low1.Process(cv2)
        cv3 = CandleIndicatorValue(self._high2, candle)
        h2 = self._high2.Process(cv3)
        cv4 = CandleIndicatorValue(self._low2, candle)
        l2 = self._low2.Process(cv4)
        cv5 = CandleIndicatorValue(self._high3, candle)
        h3 = self._high3.Process(cv5)
        cv6 = CandleIndicatorValue(self._low3, candle)
        l3 = self._low3.Process(cv6)

        if (not h1.IsFormed or not l1.IsFormed or not h2.IsFormed or
            not l2.IsFormed or not h3.IsFormed or not l3.IsFormed):
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        high1 = float(h1)
        low1 = float(l1)
        high2 = float(h2)
        low2 = float(l2)
        high3 = float(h3)
        low3 = float(l3)

        mid = (float(candle.HighPrice) + float(candle.LowPrice)) / 2.0

        hlr1 = 0.0 if (high1 - low1) == 0 else 100.0 * (mid - low1) / (high1 - low1)
        hlr2 = 0.0 if (high2 - low2) == 0 else 100.0 * (mid - low2) / (high2 - low2)
        hlr3 = 0.0 if (high3 - low3) == 0 else 100.0 * (mid - low3) / (high3 - low3)

        fast = self._factor1.Value * hlr1 + self._factor2.Value * hlr2 + self._factor3.Value * hlr3
        slow = fast / self._smoothing.Value + self._prev_slow * self._smooth_const

        if self._is_first:
            self._prev_fast = fast
            self._prev_slow = slow
            self._is_first = False
            return

        if self._prev_fast > self._prev_slow and fast < slow and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif self._prev_fast < self._prev_slow and fast > slow and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return color_zerolag_hlr_strategy()
