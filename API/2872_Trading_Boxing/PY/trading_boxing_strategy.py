import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class trading_boxing_strategy(Strategy):
    """CCI momentum: buy below -100, sell above 100."""
    def __init__(self):
        super(trading_boxing_strategy, self).__init__()
        self._cci_length = self.Param("CciLength", 20).SetGreaterThanZero().SetDisplay("CCI Length", "CCI period", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Candles", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnStarted(self, time):
        super(trading_boxing_strategy, self).OnStarted(time)

        cci = CommodityChannelIndex()
        cci.Length = self._cci_length.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(cci, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, cci_val):
        if candle.State != CandleStates.Finished:
            return

        if cci_val < -100 and self.Position <= 0:
            self.BuyMarket()
        elif cci_val > 100 and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return trading_boxing_strategy()
