import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Sides


class martin_for_small_deposits_strategy(Strategy):
    def __init__(self):
        super(martin_for_small_deposits_strategy, self).__init__()

        self._initial_volume = self.Param("InitialVolume", 0.01) \
            .SetDisplay("Initial Volume", "Base lot size for the first order", "Position Sizing")
        self._take_profit_pips = self.Param("TakeProfitPips", 200) \
            .SetDisplay("Initial Volume", "Base lot size for the first order", "Position Sizing")
        self._step_pips = self.Param("StepPips", 100) \
            .SetDisplay("Initial Volume", "Base lot size for the first order", "Position Sizing")
        self._bars_to_skip = self.Param("BarsToSkip", 100) \
            .SetDisplay("Initial Volume", "Base lot size for the first order", "Position Sizing")
        self._increase_factor = self.Param("IncreaseFactor", 1.7) \
            .SetDisplay("Initial Volume", "Base lot size for the first order", "Position Sizing")
        self._max_volume = self.Param("MaxVolume", 6) \
            .SetDisplay("Initial Volume", "Base lot size for the first order", "Position Sizing")
        self._min_profit = self.Param("MinProfit", 10) \
            .SetDisplay("Initial Volume", "Base lot size for the first order", "Position Sizing")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Initial Volume", "Base lot size for the first order", "Position Sizing")

        self._position_volume = 0.0
        self._avg_price = 0.0
        self._extreme_price = 0.0
        self._last_entry_price = 0.0
        self._current_trade_count = 0.0
        self._current_direction = 0.0
        self._bars_since_last_entry = 0.0
        self._pending_open_volume = 0.0
        self._pending_open_direction = 0.0
        self._pending_close_volume = 0.0
        self._pending_close_direction = 0.0
        self._pip_size = 0.0
        self._close_history_count = 0.0
        self._latest_index = -1

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(martin_for_small_deposits_strategy, self).OnReseted()
        self._position_volume = 0.0
        self._avg_price = 0.0
        self._extreme_price = 0.0
        self._last_entry_price = 0.0
        self._current_trade_count = 0.0
        self._current_direction = 0.0
        self._bars_since_last_entry = 0.0
        self._pending_open_volume = 0.0
        self._pending_open_direction = 0.0
        self._pending_close_volume = 0.0
        self._pending_close_direction = 0.0
        self._pip_size = 0.0
        self._close_history_count = 0.0
        self._latest_index = -1

    def OnStarted(self, time):
        super(martin_for_small_deposits_strategy, self).OnStarted(time)


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
        return martin_for_small_deposits_strategy()
