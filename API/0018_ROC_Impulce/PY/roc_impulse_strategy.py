import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Momentum
from StockSharp.Algo.Strategies import Strategy

class roc_impulse_strategy(Strategy):
    """
    Strategy based on Rate of Change / Momentum impulse.
    Uses Momentum indicator crossing zero as signal for entries.
    """

    def __init__(self):
        super(roc_impulse_strategy, self).__init__()
        self._roc_period = self.Param("RocPeriod", 12) \
            .SetDisplay("Momentum Period", "Period for Momentum calculation", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_mom = 0.0
        self._has_prev_values = False
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(roc_impulse_strategy, self).OnReseted()
        self._prev_mom = 0.0
        self._has_prev_values = False
        self._cooldown = 0

    def OnStarted(self, time):
        super(roc_impulse_strategy, self).OnStarted(time)

        momentum = Momentum()
        momentum.Length = self._roc_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(momentum, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, momentum)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, mom_value):
        if candle.State != CandleStates.Finished:
            return

        mom = float(mom_value)

        if not self._has_prev_values:
            self._has_prev_values = True
            self._prev_mom = mom
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_mom = mom
            return

        if self._prev_mom <= 0 and mom > 0 and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._cooldown = 55
        elif self._prev_mom >= 0 and mom < 0 and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._cooldown = 55

        self._prev_mom = mom

    def CreateClone(self):
        return roc_impulse_strategy()
