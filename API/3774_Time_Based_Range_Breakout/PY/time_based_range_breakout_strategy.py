import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class time_based_range_breakout_strategy(Strategy):
    def __init__(self):
        super(time_based_range_breakout_strategy, self).__init__()

        self._check_hour = self.Param("CheckHour", 8) \
            .SetDisplay("Check Hour", "Hour of the day used for daily calculations", "Schedule")
        self._check_minute = self.Param("CheckMinute", 0) \
            .SetDisplay("Check Hour", "Hour of the day used for daily calculations", "Schedule")
        self._days_to_check = self.Param("DaysToCheck", 7) \
            .SetDisplay("Check Hour", "Hour of the day used for daily calculations", "Schedule")
        self._check_mode = self.Param("CheckMode", 1) \
            .SetDisplay("Check Hour", "Hour of the day used for daily calculations", "Schedule")
        self._profit_factor = self.Param("ProfitFactor", 2) \
            .SetDisplay("Check Hour", "Hour of the day used for daily calculations", "Schedule")
        self._loss_factor = self.Param("LossFactor", 2) \
            .SetDisplay("Check Hour", "Hour of the day used for daily calculations", "Schedule")
        self._offset_factor = self.Param("OffsetFactor", 2) \
            .SetDisplay("Check Hour", "Hour of the day used for daily calculations", "Schedule")
        self._close_mode = self.Param("CloseMode", 1) \
            .SetDisplay("Check Hour", "Hour of the day used for daily calculations", "Schedule")
        self._trades_per_day = self.Param("TradesPerDay", 1) \
            .SetDisplay("Check Hour", "Hour of the day used for daily calculations", "Schedule")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Check Hour", "Hour of the day used for daily calculations", "Schedule")
        self._last_open_hour = self.Param("LastOpenHour", 23) \
            .SetDisplay("Check Hour", "Hour of the day used for daily calculations", "Schedule")

        self._range_history = None
        self._close_diff_history = None
        self._current_day = None
        self._levels_day = None
        self._day_high = 0.0
        self._day_low = 0.0
        self._buy_breakout = 0.0
        self._sell_breakout = 0.0
        self._profit_distance = 0.0
        self._loss_distance = 0.0
        self._previous_check_close = None
        self._current_check_close = None
        self._trades_opened_today = 0.0
        self._levels_ready = False
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(time_based_range_breakout_strategy, self).OnReseted()
        self._range_history = None
        self._close_diff_history = None
        self._current_day = None
        self._levels_day = None
        self._day_high = 0.0
        self._day_low = 0.0
        self._buy_breakout = 0.0
        self._sell_breakout = 0.0
        self._profit_distance = 0.0
        self._loss_distance = 0.0
        self._previous_check_close = None
        self._current_check_close = None
        self._trades_opened_today = 0.0
        self._levels_ready = False
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(time_based_range_breakout_strategy, self).OnStarted(time)
        self.StartProtection(None, None)


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
        return time_based_range_breakout_strategy()
