import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Sides


class smc_hilo_max_min_strategy(Strategy):
    def __init__(self):
        super(smc_hilo_max_min_strategy, self).__init__()

        self._set_hour = self.Param("SetHour", 15) \
            .SetDisplay("Trigger Hour", "Terminal hour when pending orders are created", "Timing")
        self._take_profit_pips = self.Param("TakeProfitPips", 500) \
            .SetDisplay("Trigger Hour", "Terminal hour when pending orders are created", "Timing")
        self._stop_loss_pips = self.Param("StopLossPips", 30) \
            .SetDisplay("Trigger Hour", "Terminal hour when pending orders are created", "Timing")
        self._trailing_stop_pips = self.Param("TrailingStopPips", 30) \
            .SetDisplay("Trigger Hour", "Terminal hour when pending orders are created", "Timing")
        self._min_stop_distance_pips = self.Param("MinStopDistancePips", 0) \
            .SetDisplay("Trigger Hour", "Terminal hour when pending orders are created", "Timing")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1) \
            .SetDisplay("Trigger Hour", "Terminal hour when pending orders are created", "Timing")

        self._previous_candle = None
        self._last_setup_date = None
        self._buy_stop_order = None
        self._sell_stop_order = None
        self._long_stop_order = None
        self._long_take_profit_order = None
        self._short_stop_order = None
        self._short_take_profit_order = None
        self._best_bid = None
        self._best_ask = None
        self._long_entry_price = None
        self._short_entry_price = None
        self._long_stop_price = None
        self._short_stop_price = None
        self._long_target_price = None
        self._short_target_price = None
        self._pending_long_stop = None
        self._pending_short_stop = None
        self._pending_long_target = None
        self._pending_short_target = None
        self._pip_size = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(smc_hilo_max_min_strategy, self).OnReseted()
        self._previous_candle = None
        self._last_setup_date = None
        self._buy_stop_order = None
        self._sell_stop_order = None
        self._long_stop_order = None
        self._long_take_profit_order = None
        self._short_stop_order = None
        self._short_take_profit_order = None
        self._best_bid = None
        self._best_ask = None
        self._long_entry_price = None
        self._short_entry_price = None
        self._long_stop_price = None
        self._short_stop_price = None
        self._long_target_price = None
        self._short_target_price = None
        self._pending_long_stop = None
        self._pending_short_stop = None
        self._pending_long_target = None
        self._pending_short_target = None
        self._pip_size = 0.0

    def OnStarted(self, time):
        super(smc_hilo_max_min_strategy, self).OnStarted(time)


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
        return smc_hilo_max_min_strategy()
