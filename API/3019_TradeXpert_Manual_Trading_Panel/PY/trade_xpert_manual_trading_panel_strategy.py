import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class trade_xpert_manual_trading_panel_strategy(Strategy):
    """CCI zero-line crossover."""
    def __init__(self):
        super(trade_xpert_manual_trading_panel_strategy, self).__init__()
        self._cci_period = self.Param("CciPeriod", 20).SetGreaterThanZero().SetDisplay("CCI Period", "CCI lookback", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1).TimeFrame()).SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(trade_xpert_manual_trading_panel_strategy, self).OnReseted()
        self._prev_cci = None

    def OnStarted(self, time):
        super(trade_xpert_manual_trading_panel_strategy, self).OnStarted(time)
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

        if self._prev_cci is None:
            self._prev_cci = float(cci_val)
            return

        cci_f = float(cci_val)

        if self._prev_cci < 0 and cci_f >= 0 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif self._prev_cci > 0 and cci_f <= 0 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_cci = cci_f

    def CreateClone(self):
        return trade_xpert_manual_trading_panel_strategy()
