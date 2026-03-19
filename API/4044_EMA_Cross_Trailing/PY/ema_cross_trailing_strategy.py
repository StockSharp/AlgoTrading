import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class ema_cross_trailing_strategy(Strategy):
    """
    EMA crossover strategy with trailing stop.
    Buys when fast EMA crosses above slow EMA, sells on opposite crossover.
    """

    def __init__(self):
        super(ema_cross_trailing_strategy, self).__init__()
        self._fast_ema_length = self.Param("FastEmaLength", 5) \
            .SetDisplay("Fast EMA", "Length of the fast EMA", "Indicator")
        self._slow_ema_length = self.Param("SlowEmaLength", 60) \
            .SetDisplay("Slow EMA", "Length of the slow EMA", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Time frame for candles and EMAs", "General")

        self._current_direction = 0
        self._has_initial_direction = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ema_cross_trailing_strategy, self).OnReseted()
        self._current_direction = 0
        self._has_initial_direction = False

    def OnStarted(self, time):
        super(ema_cross_trailing_strategy, self).OnStarted(time)

        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self._fast_ema_length.Value
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self._slow_ema_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_ema, slow_ema, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return

        fast_val = float(fast_val)
        slow_val = float(slow_val)

        if fast_val > slow_val:
            new_direction = 1
        elif fast_val < slow_val:
            new_direction = -1
        else:
            return

        if not self._has_initial_direction:
            self._current_direction = new_direction
            self._has_initial_direction = True
            return

        if new_direction == self._current_direction:
            return

        prev_direction = self._current_direction
        self._current_direction = new_direction

        if new_direction == 1 and prev_direction == -1:
            if self.Position < 0:
                self.BuyMarket()
            if self.Position <= 0:
                self.BuyMarket()
        elif new_direction == -1 and prev_direction == 1:
            if self.Position > 0:
                self.SellMarket()
            if self.Position >= 0:
                self.SellMarket()

    def CreateClone(self):
        return ema_cross_trailing_strategy()
