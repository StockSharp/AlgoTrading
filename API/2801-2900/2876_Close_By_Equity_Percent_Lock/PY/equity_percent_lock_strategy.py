import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Momentum
from StockSharp.Algo.Strategies import Strategy


class equity_percent_lock_strategy(Strategy):
    def __init__(self):
        super(equity_percent_lock_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candles", "General")
        self._momentum_length = self.Param("MomentumLength", 10) \
            .SetDisplay("Momentum Length", "Momentum period", "Indicators")

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def MomentumLength(self):
        return self._momentum_length.Value

    def OnStarted2(self, time):
        super(equity_percent_lock_strategy, self).OnStarted2(time)

        momentum = Momentum()
        momentum.Length = self.MomentumLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(momentum, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, mom_value):
        if candle.State != CandleStates.Finished:
            return
        mv = float(mom_value)
        if mv > 0 and self.Position <= 0:
            self.BuyMarket()
        elif mv < 0 and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return equity_percent_lock_strategy()
