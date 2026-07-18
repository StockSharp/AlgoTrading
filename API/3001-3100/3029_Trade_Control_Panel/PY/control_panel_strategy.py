import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Momentum
from StockSharp.Algo.Strategies import Strategy


class control_panel_strategy(Strategy):
    def __init__(self):
        super(control_panel_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._mom_period = self.Param("MomPeriod", 10) \
            .SetDisplay("Momentum Period", "Momentum lookback", "Indicators")

        self._prev_mom = None

    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def MomPeriod(self):
        return self._mom_period.Value

    def OnReseted(self):
        super(control_panel_strategy, self).OnReseted()
        self._prev_mom = None

    def OnStarted2(self, time):
        super(control_panel_strategy, self).OnStarted2(time)
        self._prev_mom = None
        mom = Momentum()
        mom.Length = self.MomPeriod
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(mom, self._on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, mom_value):
        if candle.State != CandleStates.Finished:
            return
        mv = float(mom_value)
        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_mom = mv
            return
        if self._prev_mom is None:
            self._prev_mom = mv
            return
        if self._prev_mom < 100.0 and mv >= 100.0 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif self._prev_mom > 100.0 and mv <= 100.0 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        self._prev_mom = mv

    def CreateClone(self):
        return control_panel_strategy()
