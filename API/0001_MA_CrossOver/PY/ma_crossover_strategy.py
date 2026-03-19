import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class ma_crossover_strategy(Strategy):
    """
    Fast/slow EMA crossover strategy. Enters long on golden cross, short on death cross.
    """

    def __init__(self):
        super(ma_crossover_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 100).SetDisplay("Fast MA", "Fast EMA period", "MA Settings")
        self._slow_length = self.Param("SlowLength", 400).SetDisplay("Slow MA", "Slow EMA period", "MA Settings")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Timeframe", "General")

        self._was_fast_below = False
        self._is_init = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ma_crossover_strategy, self).OnReseted()
        self._was_fast_below = False
        self._is_init = False

    def OnStarted(self, time):
        super(ma_crossover_strategy, self).OnStarted(time)
        fast = ExponentialMovingAverage()
        fast.Length = self._fast_length.Value
        slow = ExponentialMovingAverage()
        slow.Length = self._slow_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        fast = float(fast_val)
        slow = float(slow_val)
        is_fast_below = fast < slow
        if not self._is_init:
            self._was_fast_below = is_fast_below
            self._is_init = True
            return
        if self._was_fast_below != is_fast_below:
            if not is_fast_below:
                if self.Position <= 0:
                    self.BuyMarket()
            else:
                if self.Position >= 0:
                    self.SellMarket()
            self._was_fast_below = is_fast_below

    def CreateClone(self):
        return ma_crossover_strategy()
