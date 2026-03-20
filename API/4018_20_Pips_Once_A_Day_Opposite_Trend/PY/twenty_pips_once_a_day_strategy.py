import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class twenty_pips_once_a_day_strategy(Strategy):
    def __init__(self):
        super(twenty_pips_once_a_day_strategy, self).__init__()

        self._fixed_volume = self.Param("FixedVolume", 0.1) \
            .SetDisplay("Fixed Volume", "Fixed trading volume (set to 0 to use risk based sizing)", "Risk")
        self._min_volume = self.Param("MinVolume", 0.1) \
            .SetDisplay("Fixed Volume", "Fixed trading volume (set to 0 to use risk based sizing)", "Risk")
        self._max_volume = self.Param("MaxVolume", 5) \
            .SetDisplay("Fixed Volume", "Fixed trading volume (set to 0 to use risk based sizing)", "Risk")
        self._risk_percent = self.Param("RiskPercent", 5) \
            .SetDisplay("Fixed Volume", "Fixed trading volume (set to 0 to use risk based sizing)", "Risk")
        self._max_orders = self.Param("MaxOrders", 1) \
            .SetDisplay("Fixed Volume", "Fixed trading volume (set to 0 to use risk based sizing)", "Risk")
        self._trading_hour = self.Param("TradingHour", 7) \
            .SetDisplay("Fixed Volume", "Fixed trading volume (set to 0 to use risk based sizing)", "Risk")
        self._trading_day_hours = self.Param("TradingDayHours", "0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23") \
            .SetDisplay("Fixed Volume", "Fixed trading volume (set to 0 to use risk based sizing)", "Risk")
        self._hours_to_check_trend = self.Param("HoursToCheckTrend", 30) \
            .SetDisplay("Fixed Volume", "Fixed trading volume (set to 0 to use risk based sizing)", "Risk")
        self._order_max_age_seconds = self.Param("OrderMaxAgeSeconds", 75600) \
            .SetDisplay("Fixed Volume", "Fixed trading volume (set to 0 to use risk based sizing)", "Risk")
        self._first_multiplier = self.Param("FirstMultiplier", 4) \
            .SetDisplay("Fixed Volume", "Fixed trading volume (set to 0 to use risk based sizing)", "Risk")
        self._second_multiplier = self.Param("SecondMultiplier", 2) \
            .SetDisplay("Fixed Volume", "Fixed trading volume (set to 0 to use risk based sizing)", "Risk")
        self._third_multiplier = self.Param("ThirdMultiplier", 5) \
            .SetDisplay("Fixed Volume", "Fixed trading volume (set to 0 to use risk based sizing)", "Risk")
        self._fourth_multiplier = self.Param("FourthMultiplier", 5) \
            .SetDisplay("Fixed Volume", "Fixed trading volume (set to 0 to use risk based sizing)", "Risk")
        self._fifth_multiplier = self.Param("FifthMultiplier", 1) \
            .SetDisplay("Fixed Volume", "Fixed trading volume (set to 0 to use risk based sizing)", "Risk")
        self._stop_loss_pips = self.Param("StopLossPips", 50) \
            .SetDisplay("Fixed Volume", "Fixed trading volume (set to 0 to use risk based sizing)", "Risk")
        self._trailing_stop_pips = self.Param("TrailingStopPips", 0) \
            .SetDisplay("Fixed Volume", "Fixed trading volume (set to 0 to use risk based sizing)", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", 10) \
            .SetDisplay("Fixed Volume", "Fixed trading volume (set to 0 to use risk based sizing)", "Risk")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1) \
            .SetDisplay("Fixed Volume", "Fixed trading volume (set to 0 to use risk based sizing)", "Risk")

        self._close_history = new()
        self._recent_losses = new(5)
        self._allowed_hours = new()
        self._sma = None
        self._last_trade_bar_time = None
        self._entry_time = None
        self._entry_price = None
        self._stop_price = None
        self._take_profit_price = None
        self._entry_volume = 0.0
        self._position_direction = 0.0
        self._pip_size = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(twenty_pips_once_a_day_strategy, self).OnReseted()
        self._close_history = new()
        self._recent_losses = new(5)
        self._allowed_hours = new()
        self._sma = None
        self._last_trade_bar_time = None
        self._entry_time = None
        self._entry_price = None
        self._stop_price = None
        self._take_profit_price = None
        self._entry_volume = 0.0
        self._position_direction = 0.0
        self._pip_size = 0.0

    def OnStarted(self, time):
        super(twenty_pips_once_a_day_strategy, self).OnStarted(time)
        self.StartProtection(None, None)

        self.__sma = SimpleMovingAverage()
        self.__sma.Length = 2

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__sma, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return twenty_pips_once_a_day_strategy()
