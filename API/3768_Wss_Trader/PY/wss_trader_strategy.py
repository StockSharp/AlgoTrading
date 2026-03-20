import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class wss_trader_strategy(Strategy):
    def __init__(self):
        super(wss_trader_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1) \
            .SetDisplay("Working Candle", "Primary candle type for trading logic.", "General")
        self._daily_candle_type = self.Param("DailyCandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Working Candle", "Primary candle type for trading logic.", "General")
        self._start_hour = self.Param("StartHour", 0) \
            .SetDisplay("Working Candle", "Primary candle type for trading logic.", "General")
        self._end_hour = self.Param("EndHour", 23) \
            .SetDisplay("Working Candle", "Primary candle type for trading logic.", "General")
        self._metric_points = self.Param("MetricPoints", 20) \
            .SetDisplay("Working Candle", "Primary candle type for trading logic.", "General")
        self._trailing_points = self.Param("TrailingPoints", 20) \
            .SetDisplay("Working Candle", "Primary candle type for trading logic.", "General")
        self._order_volume = self.Param("OrderVolume", 0.1) \
            .SetDisplay("Working Candle", "Primary candle type for trading logic.", "General")

        self._previous_daily_candle = None
        self._price_step = 0.0
        self._long_entry_level = 0.0
        self._short_entry_level = 0.0
        self._long_stop_level = 0.0
        self._short_stop_level = 0.0
        self._long_target_level = 0.0
        self._short_target_level = 0.0
        self._previous_close = 0.0
        self._has_previous_close = False
        self._levels_ready = False
        self._can_trade = False
        self._last_candle_open_time = None
        self._long_entry_price = 0.0
        self._short_entry_price = 0.0
        self._long_stop = 0.0
        self._short_stop = 0.0
        self._long_target = 0.0
        self._short_target = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(wss_trader_strategy, self).OnReseted()
        self._previous_daily_candle = None
        self._price_step = 0.0
        self._long_entry_level = 0.0
        self._short_entry_level = 0.0
        self._long_stop_level = 0.0
        self._short_stop_level = 0.0
        self._long_target_level = 0.0
        self._short_target_level = 0.0
        self._previous_close = 0.0
        self._has_previous_close = False
        self._levels_ready = False
        self._can_trade = False
        self._last_candle_open_time = None
        self._long_entry_price = 0.0
        self._short_entry_price = 0.0
        self._long_stop = 0.0
        self._short_stop = 0.0
        self._long_target = 0.0
        self._short_target = 0.0

    def OnStarted(self, time):
        super(wss_trader_strategy, self).OnStarted(time)
        self.StartProtection(None, None)


        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        daily_subscription = self.SubscribeCandles(Dailyself.candle_type)
        daily_subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return wss_trader_strategy()
