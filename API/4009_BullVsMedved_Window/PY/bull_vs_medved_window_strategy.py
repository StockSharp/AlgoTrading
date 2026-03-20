import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Sides


class bull_vs_medved_window_strategy(Strategy):
    def __init__(self):
        super(bull_vs_medved_window_strategy, self).__init__()

        self._order_volume = self.Param("OrderVolume", 0.1) \
            .SetDisplay("Order Volume", "Volume for pending orders", "Trading")
        self._candle_size_points = self.Param("CandleSizePoints", 75) \
            .SetDisplay("Order Volume", "Volume for pending orders", "Trading")
        self._stop_loss_multiplier = self.Param("StopLossMultiplier", 0.8) \
            .SetDisplay("Order Volume", "Volume for pending orders", "Trading")
        self._take_profit_multiplier = self.Param("TakeProfitMultiplier", 0.8) \
            .SetDisplay("Order Volume", "Volume for pending orders", "Trading")
        self._buy_indent_points = self.Param("BuyIndentPoints", 16) \
            .SetDisplay("Order Volume", "Volume for pending orders", "Trading")
        self._sell_indent_points = self.Param("SellIndentPoints", 20) \
            .SetDisplay("Order Volume", "Volume for pending orders", "Trading")
        self._entry_window_minutes = self.Param("EntryWindowMinutes", 5) \
            .SetDisplay("Order Volume", "Volume for pending orders", "Trading")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Order Volume", "Volume for pending orders", "Trading")
        self._start_time0 = self.Param("StartTime0", new TimeSpan(0, 5, 0) \
            .SetDisplay("Order Volume", "Volume for pending orders", "Trading")
        self._start_time1 = self.Param("StartTime1", new TimeSpan(4, 5, 0) \
            .SetDisplay("Order Volume", "Volume for pending orders", "Trading")
        self._start_time2 = self.Param("StartTime2", new TimeSpan(8, 5, 0) \
            .SetDisplay("Order Volume", "Volume for pending orders", "Trading")
        self._start_time3 = self.Param("StartTime3", new TimeSpan(12, 5, 0) \
            .SetDisplay("Order Volume", "Volume for pending orders", "Trading")
        self._start_time4 = self.Param("StartTime4", new TimeSpan(16, 5, 0) \
            .SetDisplay("Order Volume", "Volume for pending orders", "Trading")
        self._start_time5 = self.Param("StartTime5", new TimeSpan(20, 5, 0) \
            .SetDisplay("Order Volume", "Volume for pending orders", "Trading")

        self._point_value = 0.0
        self._candle_size_threshold = 0.0
        self._body_min_size = 0.0
        self._pullback_size = 0.0
        self._buy_indent = 0.0
        self._sell_indent = 0.0
        self._previous_candle1 = None
        self._previous_candle2 = None
        self._entry_window = TimeSpan.Zero
        self._pending_lifetime = TimeSpan.FromMinutes(230)
        self._best_bid = 0.0
        self._best_ask = 0.0
        self._order_placed_in_window = False
        self._entry_order = None
        self._entry_order_id = 0.0
        self._pending_order_side = None
        self._pending_stop_distance = 0.0
        self._pending_take_distance = 0.0
        self._long_stop_price = None
        self._long_take_price = None
        self._short_stop_price = None
        self._short_take_price = None
        self._exit_requested = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(bull_vs_medved_window_strategy, self).OnReseted()
        self._point_value = 0.0
        self._candle_size_threshold = 0.0
        self._body_min_size = 0.0
        self._pullback_size = 0.0
        self._buy_indent = 0.0
        self._sell_indent = 0.0
        self._previous_candle1 = None
        self._previous_candle2 = None
        self._entry_window = TimeSpan.Zero
        self._pending_lifetime = TimeSpan.FromMinutes(230)
        self._best_bid = 0.0
        self._best_ask = 0.0
        self._order_placed_in_window = False
        self._entry_order = None
        self._entry_order_id = 0.0
        self._pending_order_side = None
        self._pending_stop_distance = 0.0
        self._pending_take_distance = 0.0
        self._long_stop_price = None
        self._long_take_price = None
        self._short_stop_price = None
        self._short_take_price = None
        self._exit_requested = False

    def OnStarted(self, time):
        super(bull_vs_medved_window_strategy, self).OnStarted(time)


        candle_subscription = self.SubscribeCandles(self.candle_type)
        candle_subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return bull_vs_medved_window_strategy()
