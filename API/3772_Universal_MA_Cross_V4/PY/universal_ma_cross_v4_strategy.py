import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import DecimalIndicatorValue, ExponentialMovingAverage, SimpleMovingAverage, SmoothedMovingAverage, WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class universal_ma_cross_v4_strategy(Strategy):
    def __init__(self):
        super(universal_ma_cross_v4_strategy, self).__init__()

        self._fast_ma_period = self.Param("FastMaPeriod", 10) \
            .SetDisplay("Fast MA Period", "Length of the fast moving average", "Indicators")
        self._slow_ma_period = self.Param("SlowMaPeriod", 80) \
            .SetDisplay("Fast MA Period", "Length of the fast moving average", "Indicators")
        self._fast_ma_type = self.Param("FastMaType", MovingAverageMethods.Exponential) \
            .SetDisplay("Fast MA Period", "Length of the fast moving average", "Indicators")
        self._slow_ma_type = self.Param("SlowMaType", MovingAverageMethods.Exponential) \
            .SetDisplay("Fast MA Period", "Length of the fast moving average", "Indicators")
        self._fast_price_type = self.Param("FastPriceType", AppliedPrices.Close) \
            .SetDisplay("Fast MA Period", "Length of the fast moving average", "Indicators")
        self._slow_price_type = self.Param("SlowPriceType", AppliedPrices.Close) \
            .SetDisplay("Fast MA Period", "Length of the fast moving average", "Indicators")
        self._stop_loss_points = self.Param("StopLossPoints", 100) \
            .SetDisplay("Fast MA Period", "Length of the fast moving average", "Indicators")
        self._take_profit_points = self.Param("TakeProfitPoints", 200) \
            .SetDisplay("Fast MA Period", "Length of the fast moving average", "Indicators")
        self._trailing_stop_points = self.Param("TrailingStopPoints", 40) \
            .SetDisplay("Fast MA Period", "Length of the fast moving average", "Indicators")
        self._min_cross_distance_points = self.Param("MinCrossDistancePoints", 0) \
            .SetDisplay("Fast MA Period", "Length of the fast moving average", "Indicators")
        self._reverse_condition = self.Param("ReverseCondition", False) \
            .SetDisplay("Fast MA Period", "Length of the fast moving average", "Indicators")
        self._confirmed_on_entry = self.Param("ConfirmedOnEntry", True) \
            .SetDisplay("Fast MA Period", "Length of the fast moving average", "Indicators")
        self._one_entry_per_bar = self.Param("OneEntryPerBar", True) \
            .SetDisplay("Fast MA Period", "Length of the fast moving average", "Indicators")
        self._stop_and_reverse = self.Param("StopAndReverse", True) \
            .SetDisplay("Fast MA Period", "Length of the fast moving average", "Indicators")
        self._pure_sar = self.Param("PureSar", False) \
            .SetDisplay("Fast MA Period", "Length of the fast moving average", "Indicators")
        self._use_hour_trade = self.Param("UseHourTrade", False) \
            .SetDisplay("Fast MA Period", "Length of the fast moving average", "Indicators")
        self._start_hour = self.Param("StartHour", 10) \
            .SetDisplay("Fast MA Period", "Length of the fast moving average", "Indicators")
        self._end_hour = self.Param("EndHour", 11) \
            .SetDisplay("Fast MA Period", "Length of the fast moving average", "Indicators")
        self._volume = self.Param("TradeVolume", 1) \
            .SetDisplay("Fast MA Period", "Length of the fast moving average", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Fast MA Period", "Length of the fast moving average", "Indicators")

        self._fast_ma = None
        self._slow_ma = None
        self._fast_prev = None
        self._fast_prev_prev = None
        self._slow_prev = None
        self._slow_prev_prev = None
        self._last_entry_bar = None
        self._last_trade = TradeDirections.None
        self._entry_price = None
        self._stop_price = None
        self._take_profit_price = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(universal_ma_cross_v4_strategy, self).OnReseted()
        self._fast_ma = None
        self._slow_ma = None
        self._fast_prev = None
        self._fast_prev_prev = None
        self._slow_prev = None
        self._slow_prev_prev = None
        self._last_entry_bar = None
        self._last_trade = TradeDirections.None
        self._entry_price = None
        self._stop_price = None
        self._take_profit_price = None

    def OnStarted(self, time):
        super(universal_ma_cross_v4_strategy, self).OnStarted(time)
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
        return universal_ma_cross_v4_strategy()
