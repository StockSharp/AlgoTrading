import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Trix
from StockSharp.Algo.Strategies import Strategy


class exp_t3_trix_strategy(Strategy):
    def __init__(self):
        super(exp_t3_trix_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._period = self.Param("Period", 14) \
            .SetDisplay("Period", "TRIX period", "Indicators")

        self._prev_trix = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def Period(self):
        return self._period.Value

    def OnReseted(self):
        super(exp_t3_trix_strategy, self).OnReseted()
        self._prev_trix = None

    def OnStarted2(self, time):
        super(exp_t3_trix_strategy, self).OnStarted2(time)
        self._prev_trix = None

        trix = Trix()
        trix.Length = self.Period

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(trix, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

        ind_area = self.CreateChartArea()
        if ind_area is not None:
            self.DrawIndicator(ind_area, trix)

    def _on_process(self, candle, trix_value):
        if candle.State != CandleStates.Finished:
            return

        tv = float(trix_value)

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_trix = tv
            return

        if self._prev_trix is None:
            self._prev_trix = tv
            return

        # TRIX crosses above zero
        if self._prev_trix <= 0 and tv > 0:
            if self.Position < 0:
                self.BuyMarket()
            if self.Position <= 0:
                self.BuyMarket()
        # TRIX crosses below zero
        elif self._prev_trix >= 0 and tv < 0:
            if self.Position > 0:
                self.SellMarket()
            if self.Position >= 0:
                self.SellMarket()

        self._prev_trix = tv

    def CreateClone(self):
        return exp_t3_trix_strategy()
