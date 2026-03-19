import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy

class ea_close_strategy(Strategy):
    """
    EA Close strategy: CCI zero-line crossover.
    Buys when CCI crosses above zero, sells when CCI crosses below zero.
    """

    def __init__(self):
        super(ea_close_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._cci_period = self.Param("CciPeriod", 14) \
            .SetDisplay("CCI Period", "CCI lookback", "Indicators")

        self._prev_cci = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ea_close_strategy, self).OnReseted()
        self._prev_cci = None

    def OnStarted(self, time):
        super(ea_close_strategy, self).OnStarted(time)
        self._prev_cci = None

        cci = CommodityChannelIndex()
        cci.Length = self._cci_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(cci, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, cci_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            cci_val = float(cci_val)
            self._prev_cci = cci_val
            return

        cci_val = float(cci_val)

        if self._prev_cci is None:
            self._prev_cci = cci_val
            return

        if self._prev_cci < 0.0 and cci_val >= 0.0 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif self._prev_cci > 0.0 and cci_val <= 0.0 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_cci = cci_val

    def CreateClone(self):
        return ea_close_strategy()
