import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class plan_x_breakout_strategy(Strategy):
    """Plan X Breakout strategy using highest high / lowest low channel breakout.
    Buy when price breaks above the highest high of the lookback period.
    Sell when price breaks below the lowest low of the lookback period."""

    def __init__(self):
        super(plan_x_breakout_strategy, self).__init__()

        self._lookback = self.Param("Lookback", 20) \
            .SetDisplay("Lookback", "Channel lookback period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._prev_high = 0.0
        self._prev_low = 0.0
        self._has_prev = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def Lookback(self):
        return self._lookback.Value

    def OnReseted(self):
        super(plan_x_breakout_strategy, self).OnReseted()
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(plan_x_breakout_strategy, self).OnStarted2(time)

        self._has_prev = False

        highest = Highest()
        highest.Length = self.Lookback
        lowest = Lowest()
        lowest.Length = self.Lookback

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(highest, lowest, self._process_candle).Start()

    def _process_candle(self, candle, highest, lowest):
        if candle.State != CandleStates.Finished:
            return

        high_val = float(highest)
        low_val = float(lowest)
        close = float(candle.ClosePrice)

        if not self._has_prev:
            self._prev_high = high_val
            self._prev_low = low_val
            self._has_prev = True
            return

        # Breakout above previous highest high
        if self.Position <= 0 and close > self._prev_high:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        # Breakout below previous lowest low
        elif self.Position >= 0 and close < self._prev_low:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_high = high_val
        self._prev_low = low_val

    def CreateClone(self):
        return plan_x_breakout_strategy()
