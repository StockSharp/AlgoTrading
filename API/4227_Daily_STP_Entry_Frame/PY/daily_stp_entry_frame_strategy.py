import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class daily_stp_entry_frame_strategy(Strategy):
    def __init__(self):
        super(daily_stp_entry_frame_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Candle Type", "Time-frame for monitoring", "General")
        self._stop_loss_points = self.Param("StopLossPoints", 80) \
            .SetDisplay("Candle Type", "Time-frame for monitoring", "General")
        self._take_profit_points = self.Param("TakeProfitPoints", 200) \
            .SetDisplay("Candle Type", "Time-frame for monitoring", "General")

        self._pip_size = 0.0
        self._stop_loss_offset = 0.0
        self._take_profit_offset = 0.0
        self._previous_day_high = None
        self._previous_day_low = None
        self._current_day_high = 0.0
        self._current_day_low = 0.0
        self._current_trading_day = None
        self._traded_today = False
        self._entry_price = 0.0
        self._long_stop = None
        self._long_take = None
        self._short_stop = None
        self._short_take = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(daily_stp_entry_frame_strategy, self).OnReseted()
        self._pip_size = 0.0
        self._stop_loss_offset = 0.0
        self._take_profit_offset = 0.0
        self._previous_day_high = None
        self._previous_day_low = None
        self._current_day_high = 0.0
        self._current_day_low = 0.0
        self._current_trading_day = None
        self._traded_today = False
        self._entry_price = 0.0
        self._long_stop = None
        self._long_take = None
        self._short_stop = None
        self._short_take = None

    def OnStarted(self, time):
        super(daily_stp_entry_frame_strategy, self).OnStarted(time)


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
        return daily_stp_entry_frame_strategy()
