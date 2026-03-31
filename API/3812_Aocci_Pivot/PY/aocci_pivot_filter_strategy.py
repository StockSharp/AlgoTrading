import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex, Momentum
from StockSharp.Algo.Strategies import Strategy


class aocci_pivot_filter_strategy(Strategy):
    def __init__(self):
        super(aocci_pivot_filter_strategy, self).__init__()
        self._cci_period = self.Param("CciPeriod", 14) \
            .SetDisplay("CCI Period", "CCI period", "Indicators")
        self._momentum_period = self.Param("MomentumPeriod", 10) \
            .SetDisplay("Momentum Period", "Momentum period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._prev_cci = 0.0
        self._has_prev = False

    @property
    def cci_period(self):
        return self._cci_period.Value

    @property
    def momentum_period(self):
        return self._momentum_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(aocci_pivot_filter_strategy, self).OnReseted()
        self._prev_cci = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(aocci_pivot_filter_strategy, self).OnStarted2(time)
        self._has_prev = False
        cci = CommodityChannelIndex()
        cci.Length = self.cci_period
        mom = Momentum()
        mom.Length = self.momentum_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(cci, mom, self.process_candle).Start()

    def process_candle(self, candle, cci_value, mom_value):
        if candle.State != CandleStates.Finished:
            return
        cci_val = float(cci_value)
        mom_val = float(mom_value)
        if not self._has_prev:
            self._prev_cci = cci_val
            self._has_prev = True
            return
        if self._prev_cci <= 0 and cci_val > 0 and mom_val > 0 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif self._prev_cci >= 0 and cci_val < 0 and mom_val < 0 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        self._prev_cci = cci_val

    def CreateClone(self):
        return aocci_pivot_filter_strategy()
