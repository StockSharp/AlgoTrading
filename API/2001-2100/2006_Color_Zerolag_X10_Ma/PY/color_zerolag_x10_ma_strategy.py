import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ZeroLagExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class color_zerolag_x10_ma_strategy(Strategy):
    """
    Trend detection strategy based on the slope of a zero lag moving average.
    """

    def __init__(self):
        super(color_zerolag_x10_ma_strategy, self).__init__()
        self._length = self.Param("Length", 20) \
            .SetDisplay("Length", "MA length", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._prev1 = 0.0
        self._prev2 = 0.0
        self._count = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(color_zerolag_x10_ma_strategy, self).OnReseted()
        self._prev1 = 0.0
        self._prev2 = 0.0
        self._count = 0

    def OnStarted2(self, time):
        super(color_zerolag_x10_ma_strategy, self).OnStarted2(time)

        zlma = ZeroLagExponentialMovingAverage()
        zlma.Length = self._length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(zlma, self.on_process).Start()

    def on_process(self, candle, ma_val):
        if candle.State != CandleStates.Finished:
            return

        self._count += 1
        if self._count < 3:
            self._prev2 = self._prev1
            self._prev1 = ma_val
            return

        trend_up = self._prev1 < self._prev2 and ma_val > self._prev1
        trend_down = self._prev1 > self._prev2 and ma_val < self._prev1

        if trend_up and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif trend_down and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev2 = self._prev1
        self._prev1 = ma_val

    def CreateClone(self):
        return color_zerolag_x10_ma_strategy()
