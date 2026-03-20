import clr
import random

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class at_random_full_strategy(Strategy):
    def __init__(self):
        super(at_random_full_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._max_positions = self.Param("MaxPositions", 3)
        self._random_seed = self.Param("RandomSeed", 123)

        self._rng = None
        self._last_entry_price = 0.0
        self._entry_count = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def MaxPositions(self):
        return self._max_positions.Value

    @MaxPositions.setter
    def MaxPositions(self, value):
        self._max_positions.Value = value

    @property
    def RandomSeed(self):
        return self._random_seed.Value

    @RandomSeed.setter
    def RandomSeed(self, value):
        self._random_seed.Value = value

    def OnReseted(self):
        super(at_random_full_strategy, self).OnReseted()
        self._rng = None
        self._last_entry_price = 0.0
        self._entry_count = 0

    def OnStarted(self, time):
        super(at_random_full_strategy, self).OnStarted(time)
        seed = self.RandomSeed
        self._rng = random.Random(seed if seed != 0 else None)
        self._last_entry_price = 0.0
        self._entry_count = 0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        # Only trade occasionally
        if self._rng.randint(0, 4) != 0:
            return

        close = float(candle.ClosePrice)

        # Check grid spacing
        if self._last_entry_price > 0 and abs(close - self._last_entry_price) / self._last_entry_price < 0.005:
            return

        # Check entry limit
        if self.MaxPositions > 0 and self._entry_count >= self.MaxPositions:
            if self.Position > 0:
                self.SellMarket()
            elif self.Position < 0:
                self.BuyMarket()
            self._entry_count = 0
            self._last_entry_price = 0.0
            return

        go_long = self._rng.randint(0, 1) == 0

        if go_long:
            if self.Position <= 0:
                self.BuyMarket()
                self._last_entry_price = close
                self._entry_count += 1
        else:
            if self.Position >= 0:
                self.SellMarket()
                self._last_entry_price = close
                self._entry_count += 1

    def CreateClone(self):
        return at_random_full_strategy()
