import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy


class xcci_histogram_vol_direct_strategy(Strategy):
    def __init__(self):
        super(xcci_histogram_vol_direct_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._cci_period = self.Param("CciPeriod", 14) \
            .SetDisplay("CCI Period", "CCI lookback", "Indicators")

        self._prev_cci = None

    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def CciPeriod(self):
        return self._cci_period.Value

    def OnReseted(self):
        super(xcci_histogram_vol_direct_strategy, self).OnReseted()
        self._prev_cci = None

    def OnStarted2(self, time):
        super(xcci_histogram_vol_direct_strategy, self).OnStarted2(time)
        self._prev_cci = None

        cci = CommodityChannelIndex()
        cci.Length = self.CciPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(cci, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, cci_value):
        if candle.State != CandleStates.Finished:
            return
        cv = float(cci_value)
        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_cci = cv
            return
        if self._prev_cci is None:
            self._prev_cci = cv
            return
        if self._prev_cci < 0.0 and cv >= 0.0 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif self._prev_cci > 0.0 and cv <= 0.0 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        self._prev_cci = cv

    def CreateClone(self):
        return xcci_histogram_vol_direct_strategy()
