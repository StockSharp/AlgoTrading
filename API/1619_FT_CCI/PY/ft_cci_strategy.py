import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy


class ft_cci_strategy(Strategy):
    def __init__(self):
        super(ft_cci_strategy, self).__init__()
        self._cci_period = self.Param("CciPeriod", 14) \
            .SetDisplay("CCI Period", "Averaging period for CCI", "Indicator")
        self._upper_threshold = self.Param("UpperThreshold", 210.0) \
            .SetDisplay("CCI Upper", "CCI level for short entries", "Indicator")
        self._lower_threshold = self.Param("LowerThreshold", -210.0) \
            .SetDisplay("CCI Lower", "CCI level for long entries", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle Type", "General")

    @property
    def cci_period(self):
        return self._cci_period.Value

    @property
    def upper_threshold(self):
        return self._upper_threshold.Value

    @property
    def lower_threshold(self):
        return self._lower_threshold.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(ft_cci_strategy, self).OnStarted2(time)
        cci = CommodityChannelIndex()
        cci.Length = self.cci_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(cci, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, cci)
            self.DrawOwnTrades(area)

    def on_process(self, candle, cci_value):
        if candle.State != CandleStates.Finished:
            return
        if cci_value <= self.lower_threshold and self.Position <= 0:
            self.BuyMarket()
        elif cci_value >= self.upper_threshold and self.Position >= 0:
            self.SellMarket()
        # Exit when CCI returns to zero
        if self.Position > 0 and cci_value >= 0:
            self.SellMarket()
        elif self.Position < 0 and cci_value <= 0:
            self.BuyMarket()

    def CreateClone(self):
        return ft_cci_strategy()
