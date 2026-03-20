import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class breakeven_trailing_stop_tick_strategy(Strategy):
    def __init__(self):
        super(breakeven_trailing_stop_tick_strategy, self).__init__()

        self._trailing_stop_pips = self.Param("TrailingStopPips", 10) \
            .SetDisplay("Trailing Stop", "Trailing stop distance in pips", "Trailing")
        self._trailing_step_pips = self.Param("TrailingStepPips", 1) \
            .SetDisplay("Trailing Stop", "Trailing stop distance in pips", "Trailing")
        self._enable_demo_entries = self.Param("EnableDemoEntries", True) \
            .SetDisplay("Trailing Stop", "Trailing stop distance in pips", "Trailing")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Trailing Stop", "Trailing stop distance in pips", "Trailing")

        self._point_value = 0.0
        self._long_stop_price = None
        self._short_stop_price = None
        self._exit_order_pending = False
        self._entry_price = 0.0
        self._last_demo_entry_time = None
        self._random = new()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(breakeven_trailing_stop_tick_strategy, self).OnReseted()
        self._point_value = 0.0
        self._long_stop_price = None
        self._short_stop_price = None
        self._exit_order_pending = False
        self._entry_price = 0.0
        self._last_demo_entry_time = None
        self._random = new()

    def OnStarted(self, time):
        super(breakeven_trailing_stop_tick_strategy, self).OnStarted(time)


        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return breakeven_trailing_stop_tick_strategy()
