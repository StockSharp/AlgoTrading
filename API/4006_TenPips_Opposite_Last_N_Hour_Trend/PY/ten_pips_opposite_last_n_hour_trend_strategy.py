import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Sides


class ten_pips_opposite_last_n_hour_trend_strategy(Strategy):
    def __init__(self):
        super(ten_pips_opposite_last_n_hour_trend_strategy, self).__init__()

        self._fixed_volume = self.Param("FixedVolume", 0.1) \
            .SetDisplay("Fixed Volume", "Fixed volume for entries", "Risk")
        self._minimum_volume = self.Param("MinimumVolume", 0.1) \
            .SetDisplay("Fixed Volume", "Fixed volume for entries", "Risk")
        self._maximum_volume = self.Param("MaximumVolume", 5) \
            .SetDisplay("Fixed Volume", "Fixed volume for entries", "Risk")
        self._maximum_risk = self.Param("MaximumRisk", 0.05) \
            .SetDisplay("Fixed Volume", "Fixed volume for entries", "Risk")
        self._max_orders = self.Param("MaxOrders", 1) \
            .SetDisplay("Fixed Volume", "Fixed volume for entries", "Risk")
        self._trading_hour = self.Param("TradingHour", 7) \
            .SetDisplay("Fixed Volume", "Fixed volume for entries", "Risk")
        self._hours_to_check_trend = self.Param("HoursToCheckTrend", 30) \
            .SetDisplay("Fixed Volume", "Fixed volume for entries", "Risk")
        self._order_max_age = self.Param("OrderMaxAge", TimeSpan.FromSeconds(75600) \
            .SetDisplay("Fixed Volume", "Fixed volume for entries", "Risk")
        self._stop_loss_pips = self.Param("StopLossPips", 50) \
            .SetDisplay("Fixed Volume", "Fixed volume for entries", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", 10) \
            .SetDisplay("Fixed Volume", "Fixed volume for entries", "Risk")
        self._trailing_stop_pips = self.Param("TrailingStopPips", 0) \
            .SetDisplay("Fixed Volume", "Fixed volume for entries", "Risk")
        self._first_multiplier = self.Param("FirstMultiplier", 4) \
            .SetDisplay("Fixed Volume", "Fixed volume for entries", "Risk")
        self._second_multiplier = self.Param("SecondMultiplier", 2) \
            .SetDisplay("Fixed Volume", "Fixed volume for entries", "Risk")
        self._third_multiplier = self.Param("ThirdMultiplier", 5) \
            .SetDisplay("Fixed Volume", "Fixed volume for entries", "Risk")
        self._fourth_multiplier = self.Param("FourthMultiplier", 5) \
            .SetDisplay("Fixed Volume", "Fixed volume for entries", "Risk")
        self._fifth_multiplier = self.Param("FifthMultiplier", 1) \
            .SetDisplay("Fixed Volume", "Fixed volume for entries", "Risk")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1) \
            .SetDisplay("Fixed Volume", "Fixed volume for entries", "Risk")

        self._trading_day_hours = None
        self._closed_trade_profits = new()
        self._close_history = new()
        self._pip_size = 0.0
        self._last_bar_traded = None
        self._entry_side = None
        self._entry_volume = 0.0
        self._entry_price = None
        self._entry_time = None
        self._trailing_stop_price = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ten_pips_opposite_last_n_hour_trend_strategy, self).OnReseted()
        self._trading_day_hours = None
        self._closed_trade_profits = new()
        self._close_history = new()
        self._pip_size = 0.0
        self._last_bar_traded = None
        self._entry_side = None
        self._entry_volume = 0.0
        self._entry_price = None
        self._entry_time = None
        self._trailing_stop_price = None

    def OnStarted(self, time):
        super(ten_pips_opposite_last_n_hour_trend_strategy, self).OnStarted(time)
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
        return ten_pips_opposite_last_n_hour_trend_strategy()
