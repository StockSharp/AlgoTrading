import clr
import random

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class at_random_strategy(Strategy):
    def __init__(self):
        super(at_random_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._random_seed = self.Param("RandomSeed", 42)

        self._rng = None
        self._bar_count = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def RandomSeed(self):
        return self._random_seed.Value

    @RandomSeed.setter
    def RandomSeed(self, value):
        self._random_seed.Value = value

    def OnReseted(self):
        super(at_random_strategy, self).OnReseted()
        self._rng = None
        self._bar_count = 0

    def OnStarted(self, time):
        super(at_random_strategy, self).OnStarted(time)
        seed = self.RandomSeed
        self._rng = random.Random(seed if seed != 0 else None)
        self._bar_count = 0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._bar_count += 1

        # Only trade occasionally
        if self._rng.randint(0, 5) != 0:
            return

        go_long = self._rng.randint(0, 1) == 0

        if go_long:
            if self.Position <= 0:
                self.BuyMarket()
        else:
            if self.Position >= 0:
                self.SellMarket()

    def CreateClone(self):
        return at_random_strategy()
