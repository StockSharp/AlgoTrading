import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class simple_pivot_flip_strategy(Strategy):
    """Simple pivot-based strategy. Compares current candle open with the previous
    candle range midpoint. If open is in the upper half of the previous range, sell;
    otherwise buy. Closes existing position before reversing."""

    def __init__(self):
        super(simple_pivot_flip_strategy, self).__init__()

        self._order_volume = self.Param("OrderVolume", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Order Volume", "Market order volume used for entries", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromDays(1))) \
            .SetDisplay("Candle Type", "Type of candles used for pivot calculation", "Data")

        self._previous_high = 0.0
        self._previous_low = 0.0
        self._has_previous_candle = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def OrderVolume(self):
        return self._order_volume.Value

    def OnReseted(self):
        super(simple_pivot_flip_strategy, self).OnReseted()
        self._previous_high = 0.0
        self._previous_low = 0.0
        self._has_previous_candle = False

    def OnStarted2(self, time):
        super(simple_pivot_flip_strategy, self).OnStarted2(time)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        open_price = float(candle.OpenPrice)

        if not self._has_previous_candle:
            self._previous_high = high
            self._previous_low = low
            self._has_previous_candle = True
            return

        # Calculate the pivot as the midpoint of the previous candle range
        pivot = (self._previous_high + self._previous_low) / 2.0

        # Default to buy; if open is in upper half of previous range, sell
        desired_buy = True
        if open_price < self._previous_high and open_price > pivot:
            desired_buy = False

        # Skip re-entry if already holding position in desired direction
        if desired_buy and self.Position > 0:
            self._previous_high = high
            self._previous_low = low
            return
        if not desired_buy and self.Position < 0:
            self._previous_high = high
            self._previous_low = low
            return

        # Close existing position
        if self.Position > 0:
            self.SellMarket()
        elif self.Position < 0:
            self.BuyMarket()

        # Open new position
        if desired_buy:
            self.BuyMarket()
        else:
            self.SellMarket()

        # Update reference range for next candle
        self._previous_high = high
        self._previous_low = low

    def CreateClone(self):
        return simple_pivot_flip_strategy()
