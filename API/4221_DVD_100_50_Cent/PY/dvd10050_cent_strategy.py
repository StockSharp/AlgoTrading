import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import DecimalIndicatorValue, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Sides
from StockSharp.Messages import Unit, UnitTypes


class dvd10050_cent_strategy(Strategy):
    def __init__(self):
        super(dvd10050_cent_strategy, self).__init__()

        self._account_is_mini = self.Param("AccountIsMini", True) \
            .SetDisplay("Mini Account", "Use mini account position sizing", "Risk")
        self._use_money_management = self.Param("UseMoneyManagement", True) \
            .SetDisplay("Mini Account", "Use mini account position sizing", "Risk")
        self._trade_size_percent = self.Param("TradeSizePercent", 10) \
            .SetDisplay("Mini Account", "Use mini account position sizing", "Risk")
        self._fixed_volume = self.Param("FixedVolume", 0.01) \
            .SetDisplay("Mini Account", "Use mini account position sizing", "Risk")
        self._max_volume = self.Param("MaxVolume", 4) \
            .SetDisplay("Mini Account", "Use mini account position sizing", "Risk")
        self._stop_loss_pips = self.Param("StopLossPips", 210) \
            .SetDisplay("Mini Account", "Use mini account position sizing", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", 18) \
            .SetDisplay("Mini Account", "Use mini account position sizing", "Risk")
        self._point_from_level_go_pips = self.Param("PointFromLevelGoPips", 50) \
            .SetDisplay("Mini Account", "Use mini account position sizing", "Risk")
        self._rise_filter_pips = self.Param("RiseFilterPips", 700) \
            .SetDisplay("Mini Account", "Use mini account position sizing", "Risk")
        self._high_level_pips = self.Param("HighLevelPips", 600) \
            .SetDisplay("Mini Account", "Use mini account position sizing", "Risk")
        self._low_level_pips = self.Param("LowLevelPips", 250) \
            .SetDisplay("Mini Account", "Use mini account position sizing", "Risk")
        self._low_level2_pips = self.Param("LowLevel2Pips", 450) \
            .SetDisplay("Mini Account", "Use mini account position sizing", "Risk")
        self._margin_cutoff = self.Param("MarginCutoff", 300) \
            .SetDisplay("Mini Account", "Use mini account position sizing", "Risk")
        self._order_expiry_minutes = self.Param("OrderExpiryMinutes", 20) \
            .SetDisplay("Mini Account", "Use mini account position sizing", "Risk")
        self._m1_history_length = self.Param("M1HistoryLength", 64) \
            .SetDisplay("Mini Account", "Use mini account position sizing", "Risk")
        self._m30_history_length = self.Param("M30HistoryLength", 16) \
            .SetDisplay("Mini Account", "Use mini account position sizing", "Risk")
        self._h1_history_length = self.Param("H1HistoryLength", 16) \
            .SetDisplay("Mini Account", "Use mini account position sizing", "Risk")

        self._h1_fast = null!
        self._h1_slow = null!
        self._d1_fast = null!
        self._d1_slow = null!
        self._m1_history = new()
        self._m30_history = new()
        self._h1_finished = new()
        self._h1_current = None
        self._ravi_h1 = None
        self._ravi_d1_current = None
        self._ravi_d1_prev1 = None
        self._ravi_d1_prev2 = None
        self._ravi_d1_prev3 = None
        self._pip_size = 0.0
        self._point_value = 0.0
        self._buy_order_expiry = None
        self._sell_order_expiry = None
        self._pending_buy_stop = None
        self._pending_buy_take = None
        self._pending_sell_stop = None
        self._pending_sell_take = None
        self._long_stop = None
        self._long_take = None
        self._short_stop = None
        self._short_take = None
        self._previous_position = 0.0

    def OnReseted(self):
        super(dvd10050_cent_strategy, self).OnReseted()
        self._h1_fast = null!
        self._h1_slow = null!
        self._d1_fast = null!
        self._d1_slow = null!
        self._m1_history = new()
        self._m30_history = new()
        self._h1_finished = new()
        self._h1_current = None
        self._ravi_h1 = None
        self._ravi_d1_current = None
        self._ravi_d1_prev1 = None
        self._ravi_d1_prev2 = None
        self._ravi_d1_prev3 = None
        self._pip_size = 0.0
        self._point_value = 0.0
        self._buy_order_expiry = None
        self._sell_order_expiry = None
        self._pending_buy_stop = None
        self._pending_buy_take = None
        self._pending_sell_stop = None
        self._pending_sell_take = None
        self._long_stop = None
        self._long_take = None
        self._short_stop = None
        self._short_take = None
        self._previous_position = 0.0

    def OnStarted(self, time):
        super(dvd10050_cent_strategy, self).OnStarted(time)

        self.__h1_fast = SimpleMovingAverage()
        self.__h1_fast.Length = 2
        self.__h1_slow = SimpleMovingAverage()
        self.__h1_slow.Length = 24
        self.__d1_fast = SimpleMovingAverage()
        self.__d1_fast.Length = 2
        self.__d1_slow = SimpleMovingAverage()
        self.__d1_slow.Length = 24

        m1_subscription = self.SubscribeCandles(TimeSpan.FromMinutes(5)
        m1_subscription.Bind(self._process_candle).Start()

        m30_subscription = self.SubscribeCandles(TimeSpan.FromMinutes(30)
        m30_subscription.Bind(self._process_candle).Start()

        h1_subscription = self.SubscribeCandles(TimeSpan.FromHours(1)
        h1_subscription.Bind(self._process_candle).Start()

        d1_subscription = self.SubscribeCandles(TimeSpan.FromMinutes(5)
        d1_subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return dvd10050_cent_strategy()
