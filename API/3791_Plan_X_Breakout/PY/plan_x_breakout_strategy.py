import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class plan_x_breakout_strategy(Strategy):
    def __init__(self):
        super(plan_x_breakout_strategy, self).__init__()

        self._lookback = self.Param("Lookback", 20) \
            .SetDisplay("Lookback", "Channel lookback period", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Lookback", "Channel lookback period", "Indicators")

        self._prev_high = 0.0
        self._prev_low = 0.0
        self._has_prev = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(plan_x_breakout_strategy, self).OnReseted()
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(plan_x_breakout_strategy, self).OnStarted(time)

        self._highest_high = Highest()
        self._highest_high.Length = self.lookback
        self._lowest_low = Lowest()
        self._lowest_low.Length = self.lookback

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._highest_high, self._lowest_low, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return plan_x_breakout_strategy()
