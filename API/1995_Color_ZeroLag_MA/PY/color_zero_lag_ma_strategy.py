import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ZeroLagExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class color_zero_lag_ma_strategy(Strategy):
    """
    Strategy that follows ZLEMA direction changes for trend signals.
    """

    def __init__(self):
        super(color_zero_lag_ma_strategy, self).__init__()
        self._length = self.Param("Length", 12) \
            .SetDisplay("Length", "ZLEMA length", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._prev_zlma = 0.0
        self._prev_prev_zlma = 0.0
        self._count = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(color_zero_lag_ma_strategy, self).OnReseted()
        self._prev_zlma = 0.0
        self._prev_prev_zlma = 0.0
        self._count = 0

    def OnStarted(self, time):
        super(color_zero_lag_ma_strategy, self).OnStarted(time)

        zlma = ZeroLagExponentialMovingAverage()
        zlma.Length = self._length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(zlma, self.on_process).Start()

    def on_process(self, candle, zlma_val):
        if candle.State != CandleStates.Finished:
            return

        self._count += 1
        if self._count < 3:
            self._prev_prev_zlma = self._prev_zlma
            self._prev_zlma = zlma_val
            return

        turn_up = self._prev_zlma < self._prev_prev_zlma and zlma_val > self._prev_zlma
        turn_down = self._prev_zlma > self._prev_prev_zlma and zlma_val < self._prev_zlma

        if turn_up and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif turn_down and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_prev_zlma = self._prev_zlma
        self._prev_zlma = zlma_val

    def CreateClone(self):
        return color_zero_lag_ma_strategy()
