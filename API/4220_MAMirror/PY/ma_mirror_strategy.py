import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class ma_mirror_strategy(Strategy):
    """
    Mirror: close vs SMA crossover. Buy when close crosses above, sell below.
    """

    def __init__(self):
        super(ma_mirror_strategy, self).__init__()
        self._moving_period = self.Param("MovingPeriod", 10).SetDisplay("SMA Period", "SMA length", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Timeframe", "General")
        self._prev_diff = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ma_mirror_strategy, self).OnReseted()
        self._prev_diff = None

    def OnStarted(self, time):
        super(ma_mirror_strategy, self).OnStarted(time)
        sma = SimpleMovingAverage()
        sma.Length = self._moving_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self._process_candle).Start()

    def _process_candle(self, candle, sma_val):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        sma = float(sma_val)
        diff = close - sma
        if self._prev_diff is not None:
            if self._prev_diff <= 0 and diff > 0 and self.Position <= 0:
                self.BuyMarket()
            elif self._prev_diff >= 0 and diff < 0 and self.Position >= 0:
                self.SellMarket()
        self._prev_diff = diff

    def CreateClone(self):
        return ma_mirror_strategy()
