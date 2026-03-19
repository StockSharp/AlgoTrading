import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class grid_strategy(Strategy):
    """
    Simple grid trading strategy.
    Buys when price moves up a grid level, sells when down.
    Closes on profit target.
    """

    def __init__(self):
        super(grid_strategy, self).__init__()
        self._grid_step = self.Param("GridStep", 500.0) \
            .SetDisplay("Grid Step", "Step size in price units", "General")
        self._profit_target = self.Param("ProfitTarget", 2000.0) \
            .SetDisplay("Profit Target", "Profit to close position", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle type", "General")

        self._last_grid_level = 0.0
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(grid_strategy, self).OnReseted()
        self._last_grid_level = 0.0
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(grid_strategy, self).OnStarted(time)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        step = self._grid_step.Value
        if step <= 0:
            return

        close = float(candle.ClosePrice)
        current_level = math.floor(close / step) * step

        if self._last_grid_level == 0.0:
            self._last_grid_level = current_level
            return

        if current_level > self._last_grid_level:
            if self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
                self._entry_price = close
        elif current_level < self._last_grid_level:
            if self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()
                self._entry_price = close

        self._last_grid_level = current_level

        profit_target = self._profit_target.Value
        if self.Position > 0 and self._entry_price > 0:
            if close - self._entry_price >= profit_target:
                self.SellMarket()
                self._entry_price = 0.0
        elif self.Position < 0 and self._entry_price > 0:
            if self._entry_price - close >= profit_target:
                self.BuyMarket()
                self._entry_price = 0.0

    def CreateClone(self):
        return grid_strategy()
