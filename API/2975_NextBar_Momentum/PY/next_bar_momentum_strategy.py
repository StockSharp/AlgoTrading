import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Momentum
from StockSharp.Algo.Strategies import Strategy


class next_bar_momentum_strategy(Strategy):
    def __init__(self):
        super(next_bar_momentum_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._momentum_period = self.Param("MomentumPeriod", 10) \
            .SetDisplay("Momentum Period", "Rate of change lookback", "Indicators")

        self._prev_mom = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def MomentumPeriod(self):
        return self._momentum_period.Value

    def OnReseted(self):
        super(next_bar_momentum_strategy, self).OnReseted()
        self._prev_mom = None

    def OnStarted(self, time):
        super(next_bar_momentum_strategy, self).OnStarted(time)
        self._prev_mom = None

        momentum = Momentum()
        momentum.Length = self.MomentumPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(momentum, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
        ind_area = self.CreateChartArea()
        if ind_area is not None:
            self.DrawIndicator(ind_area, momentum)

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

        # Momentum crosses above 100
        if self._prev_mom <= 100.0 and mv > 100.0 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        # Momentum crosses below 100
        elif self._prev_mom >= 100.0 and mv < 100.0 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_mom = mv

    def CreateClone(self):
        return next_bar_momentum_strategy()
