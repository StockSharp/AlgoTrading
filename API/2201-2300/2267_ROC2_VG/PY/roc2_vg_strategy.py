import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Momentum, RateOfChange
from StockSharp.Algo.Strategies import Strategy


class roc2_vg_strategy(Strategy):
    def __init__(self):
        super(roc2_vg_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candles", "General")
        self._roc_period1 = self.Param("RocPeriod1", 8) \
            .SetDisplay("ROC Period 1", "Length of first ROC", "Indicator")
        self._roc_type1 = self.Param("RocType1", 0) \
            .SetDisplay("ROC Type 1", "Type of first ROC", "Indicator")
        self._roc_period2 = self.Param("RocPeriod2", 14) \
            .SetDisplay("ROC Period 2", "Length of second ROC", "Indicator")
        self._roc_type2 = self.Param("RocType2", 0) \
            .SetDisplay("ROC Type 2", "Type of second ROC", "Indicator")
        self._invert = self.Param("Invert", False) \
            .SetDisplay("Invert", "Swap ROC lines", "General")
        self._prev_up = None
        self._prev_dn = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def roc_period1(self):
        return self._roc_period1.Value

    @property
    def roc_type1(self):
        return self._roc_type1.Value

    @property
    def roc_period2(self):
        return self._roc_period2.Value

    @property
    def roc_type2(self):
        return self._roc_type2.Value

    @property
    def invert(self):
        return self._invert.Value

    def OnReseted(self):
        super(roc2_vg_strategy, self).OnReseted()
        self._prev_up = None
        self._prev_dn = None

    def _create_indicator(self, roc_type, period):
        if int(roc_type) == 0:
            ind = Momentum()
            ind.Length = period
            return ind
        else:
            ind = RateOfChange()
            ind.Length = period
            return ind

    def _transform(self, roc_type, value):
        t = int(roc_type)
        if t == 0:
            return value
        elif t == 1:
            return value * 100.0
        elif t == 2:
            return value
        elif t == 3:
            return value + 1.0
        elif t == 4:
            return (value + 1.0) * 100.0
        return value

    def OnStarted2(self, time):
        super(roc2_vg_strategy, self).OnStarted2(time)
        ind1 = self._create_indicator(self.roc_type1, self.roc_period1)
        ind2 = self._create_indicator(self.roc_type2, self.roc_period2)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ind1, ind2, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ind1)
            self.DrawIndicator(area, ind2)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, v1, v2):
        if candle.State != CandleStates.Finished:
            return
        v1 = float(v1)
        v2 = float(v2)
        inv = bool(self.invert)
        if inv:
            up = self._transform(self.roc_type1, v2)
            dn = self._transform(self.roc_type2, v1)
        else:
            up = self._transform(self.roc_type1, v1)
            dn = self._transform(self.roc_type2, v2)
        if self._prev_up is not None and self._prev_dn is not None:
            if self._prev_up > self._prev_dn and up <= dn and self.Position <= 0:
                self.BuyMarket()
            elif self._prev_up < self._prev_dn and up >= dn and self.Position >= 0:
                self.SellMarket()
        self._prev_up = up
        self._prev_dn = dn

    def CreateClone(self):
        return roc2_vg_strategy()
