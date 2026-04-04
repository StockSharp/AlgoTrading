import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class zakryvator_strategy(Strategy):
    def __init__(self):
        super(zakryvator_strategy, self).__init__()

        self._entry_price = 0.0
        self._last_price = 0.0
        self._prev_short_above_long = False

        self._sma_short = SimpleMovingAverage()
        self._sma_long = SimpleMovingAverage()

        self._short_period = self.Param("ShortPeriod", 50) \
            .SetDisplay("Short SMA", "Short SMA period for entry signal", "Entry")
        self._long_period = self.Param("LongPeriod", 150) \
            .SetDisplay("Long SMA", "Long SMA period for entry signal", "Entry")
        self._loss_threshold = self.Param("LossThreshold", 500.0) \
            .SetDisplay("Loss Threshold", "Max unrealized loss before closing position", "Risk")

    @property
    def ShortPeriod(self):
        return self._short_period.Value

    @property
    def LongPeriod(self):
        return self._long_period.Value

    @property
    def LossThreshold(self):
        return self._loss_threshold.Value

    def GetWorkingSecurities(self):
        return [(self.Security, DataType.TimeFrame(TimeSpan.FromMinutes(5)))]

    def OnStarted2(self, time):
        super(zakryvator_strategy, self).OnStarted2(time)

        self._sma_short.Length = self.ShortPeriod
        self._sma_long.Length = self.LongPeriod

        subscription = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        subscription.Bind(self._sma_short, self._sma_long, self.process_candle).Start()

    def process_candle(self, candle, short_sma, long_sma):
        if candle.State != CandleStates.Finished:
            return

        if not self._sma_short.IsFormed or not self._sma_long.IsFormed:
            return

        self._last_price = float(candle.ClosePrice)

        short_above_long = float(short_sma) > float(long_sma)

        # Check loss threshold for open position
        if self.Position != 0 and self._entry_price != 0.0:
            open_pnl = float(self.Position) * (self._last_price - self._entry_price)

            if open_pnl <= -float(self.LossThreshold):
                # Close on loss
                if self.Position > 0:
                    self.SellMarket()
                else:
                    self.BuyMarket()

                self._entry_price = 0.0
                self._prev_short_above_long = short_above_long
                return

        # SMA crossover entry/exit logic
        cross_up = short_above_long and not self._prev_short_above_long
        cross_down = not short_above_long and self._prev_short_above_long

        if cross_up:
            if self.Position < 0:
                self.BuyMarket()
                self._entry_price = 0.0

            if self.Position == 0:
                self.BuyMarket()
                self._entry_price = self._last_price

        elif cross_down:
            if self.Position > 0:
                self.SellMarket()
                self._entry_price = 0.0

            if self.Position == 0:
                self.SellMarket()
                self._entry_price = self._last_price

        self._prev_short_above_long = short_above_long

    def OnReseted(self):
        super(zakryvator_strategy, self).OnReseted()

        self._entry_price = 0.0
        self._last_price = 0.0
        self._prev_short_above_long = False

        self._sma_short.Reset()
        self._sma_long.Reset()

    def CreateClone(self):
        return zakryvator_strategy()
