import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class grid_bot_strategy(Strategy):
    """Grid Bot Strategy. Fixed grid of equal levels between LowerLimit and UpperLimit."""

    def __init__(self):
        super(grid_bot_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle type", "Candle type for strategy calculation.", "General")
        self._upper_limit = self.Param("UpperLimit", 48000.0) \
            .SetDisplay("Upper Limit", "Top price of the grid range", "Grid Settings")
        self._lower_limit = self.Param("LowerLimit", 45000.0) \
            .SetDisplay("Lower Limit", "Bottom price of the grid range", "Grid Settings")
        self._grid_count = self.Param("GridCount", 10) \
            .SetDisplay("Grid Count", "Number of equal levels the range is split into", "Grid Settings")

        # Grid line the previous candle closed on, -1 before the first one is evaluated.
        self._prev_level = -1

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(grid_bot_strategy, self).OnReseted()
        self._prev_level = -1

    def OnStarted2(self, time):
        super(grid_bot_strategy, self).OnStarted2(time)

        if float(self._upper_limit.Value) <= float(self._lower_limit.Value):
            raise Exception("UpperLimit must be above LowerLimit.")

        self._prev_level = -1

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        level = self._get_level(float(candle.ClosePrice))

        # A touch is the move onto another line; standing on the same one is not a new signal.
        if level == self._prev_level:
            return

        self._prev_level = level

        # The middle line splits the range into halves and carries no bias of its own.
        middle = int(self._grid_count.Value) / 2.0

        if level < middle and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif level > middle and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))

    def _get_level(self, price):
        """Index of the grid line the price sits on, counted from LowerLimit up to GridCount."""
        upper = float(self._upper_limit.Value)
        lower = float(self._lower_limit.Value)
        count = int(self._grid_count.Value)
        step = (upper - lower) / count

        # A price outside the predefined range belongs to the outermost line of the grid.
        clamped = min(max(price, lower), upper)

        return int(Math.Floor((clamped - lower) / step + 0.5))

    def CreateClone(self):
        return grid_bot_strategy()
