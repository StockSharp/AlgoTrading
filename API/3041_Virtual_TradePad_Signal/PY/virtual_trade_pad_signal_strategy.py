import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class virtual_trade_pad_signal_strategy(Strategy):
    """CCI crossing zero line for buy/sell signals."""
    def __init__(self):
        super(virtual_trade_pad_signal_strategy, self).__init__()
        self._cci_period = self.Param("CciPeriod", 20).SetGreaterThanZero().SetDisplay("CCI Period", "CCI lookback", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))).SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(virtual_trade_pad_signal_strategy, self).OnReseted()
        self._prev_cci = None

    def OnStarted(self, time):
        super(virtual_trade_pad_signal_strategy, self).OnStarted(time)
        self._prev_cci = None

        cci = CommodityChannelIndex()
        cci.Length = self._cci_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(cci, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, cci_val):
        if candle.State != CandleStates.Finished:
            return

        cv = float(cci_val)

        if self._prev_cci is not None:
            if self._prev_cci < 0 and cv >= 0 and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
            elif self._prev_cci > 0 and cv <= 0 and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()

        self._prev_cci = cv

    def CreateClone(self):
        return virtual_trade_pad_signal_strategy()
