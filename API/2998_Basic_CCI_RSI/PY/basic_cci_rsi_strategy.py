import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy


class basic_cci_rsi_strategy(Strategy):
    def __init__(self):
        super(basic_cci_rsi_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._cci_period = self.Param("CciPeriod", 20) \
            .SetDisplay("CCI Period", "CCI lookback", "Indicators")
        self._prev_cci = None

    @property
    def candle_type(self):
        return self._candle_type.Value
    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    @property
    def cci_period(self):
        return self._cci_period.Value
    @cci_period.setter
    def cci_period(self, value):
        self._cci_period.Value = value

    def OnReseted(self):
        super(basic_cci_rsi_strategy, self).OnReseted()
        self._prev_cci = None

    def OnStarted2(self, time):
        super(basic_cci_rsi_strategy, self).OnStarted2(time)
        self._prev_cci = None
        cci = CommodityChannelIndex()
        cci.Length = self.cci_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(cci, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, cci_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_cci = float(cci_val)
            return
        if self._prev_cci is None:
            self._prev_cci = float(cci_val)
            return

        if self._prev_cci < 0 and cci_val >= 0 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif self._prev_cci > 0 and cci_val <= 0 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_cci = float(cci_val)

    def CreateClone(self):
        return basic_cci_rsi_strategy()
