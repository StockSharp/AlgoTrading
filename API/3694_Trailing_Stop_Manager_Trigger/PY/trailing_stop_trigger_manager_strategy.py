import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class trailing_stop_trigger_manager_strategy(Strategy):
    def __init__(self):
        super(trailing_stop_trigger_manager_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._trailing_points = self.Param("TrailingPoints", 1000) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._trigger_points = self.Param("TriggerPoints", 1500) \
            .SetDisplay("Candle Type", "Timeframe", "General")

        self._last_entry_price = 0.0
        self._active_stop_price = None
        self._trailing_enabled = False
        self._trailing_distance = 0.0
        self._trigger_distance = 0.0
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(trailing_stop_trigger_manager_strategy, self).OnReseted()
        self._last_entry_price = 0.0
        self._active_stop_price = None
        self._trailing_enabled = False
        self._trailing_distance = 0.0
        self._trigger_distance = 0.0
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(trailing_stop_trigger_manager_strategy, self).OnStarted(time)

        self._sma_fast = SimpleMovingAverage()
        self._sma_fast.Length = 10
        self._sma_slow = SimpleMovingAverage()
        self._sma_slow.Length = 30

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._sma_fast, self._sma_slow, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return trailing_stop_trigger_manager_strategy()
