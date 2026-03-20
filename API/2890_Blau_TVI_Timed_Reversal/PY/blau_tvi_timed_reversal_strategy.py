import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Momentum
from StockSharp.Algo.Strategies import Strategy


class blau_tvi_timed_reversal_strategy(Strategy):
    def __init__(self):
        super(blau_tvi_timed_reversal_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candles", "General")
        self._momentum_length = self.Param("MomentumLength", 12) \
            .SetDisplay("Momentum Length", "Momentum period", "Indicators")

        self._prev_mom = 0.0
        self._prev_prev_mom = 0.0
        self._count = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def MomentumLength(self):
        return self._momentum_length.Value

    def OnReseted(self):
        super(blau_tvi_timed_reversal_strategy, self).OnReseted()
        self._prev_mom = 0.0
        self._prev_prev_mom = 0.0
        self._count = 0

    def OnStarted(self, time):
        super(blau_tvi_timed_reversal_strategy, self).OnStarted(time)
        self._prev_mom = 0.0
        self._prev_prev_mom = 0.0
        self._count = 0

        momentum = Momentum()
        momentum.Length = self.MomentumLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(momentum, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, momentum)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, mom_value):
        if candle.State != CandleStates.Finished:
            return
        mv = float(mom_value)
        self._count += 1
        if self._count < 3:
            self._prev_prev_mom = self._prev_mom
            self._prev_mom = mv
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_prev_mom = self._prev_mom
            self._prev_mom = mv
            return
        if self._prev_mom < self._prev_prev_mom and mv > self._prev_mom and self.Position <= 0:
            self.BuyMarket()
        elif self._prev_mom > self._prev_prev_mom and mv < self._prev_mom and self.Position >= 0:
            self.SellMarket()
        self._prev_prev_mom = self._prev_mom
        self._prev_mom = mv

    def CreateClone(self):
        return blau_tvi_timed_reversal_strategy()
