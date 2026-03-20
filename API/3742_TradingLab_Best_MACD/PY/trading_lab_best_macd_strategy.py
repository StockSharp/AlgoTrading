import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest, MovingAverageConvergenceDivergenceSignal, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Sides


class trading_lab_best_macd_strategy(Strategy):
    def __init__(self):
        super(trading_lab_best_macd_strategy, self).__init__()

        self._order_volume = self.Param("OrderVolume", 1) \
            .SetDisplay("Order Volume", "Fixed volume sent with each market order", "Risk")
        self._signal_validity = self.Param("SignalValidity", 7) \
            .SetDisplay("Order Volume", "Fixed volume sent with each market order", "Risk")
        self._ma_length = self.Param("MaLength", 20) \
            .SetDisplay("Order Volume", "Fixed volume sent with each market order", "Risk")
        self._box_period = self.Param("BoxPeriod", 20) \
            .SetDisplay("Order Volume", "Fixed volume sent with each market order", "Risk")
        self._macd_fast_length = self.Param("MacdFastLength", 12) \
            .SetDisplay("Order Volume", "Fixed volume sent with each market order", "Risk")
        self._macd_slow_length = self.Param("MacdSlowLength", 26) \
            .SetDisplay("Order Volume", "Fixed volume sent with each market order", "Risk")
        self._macd_signal_length = self.Param("MacdSignalLength", 9) \
            .SetDisplay("Order Volume", "Fixed volume sent with each market order", "Risk")
        self._stop_distance_points = self.Param("StopDistancePoints", 50) \
            .SetDisplay("Order Volume", "Fixed volume sent with each market order", "Risk")
        self._risk_reward_multiplier = self.Param("RiskRewardMultiplier", 1.5) \
            .SetDisplay("Order Volume", "Fixed volume sent with each market order", "Risk")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Order Volume", "Fixed volume sent with each market order", "Risk")

        self._sma = None
        self._highest = None
        self._lowest = None
        self._macd = None
        self._resistance_counter = 0.0
        self._support_counter = 0.0
        self._macd_down_counter = 0.0
        self._macd_up_counter = 0.0
        self._prev_macd_main = None
        self._prev_macd_signal = None
        self._planned_stop = None
        self._planned_take = None
        self._planned_side = None
        self._active_stop = None
        self._active_take = None
        self._active_side = None
        self._previous_high = None
        self._previous_low = None
        self._has_previous_candle = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(trading_lab_best_macd_strategy, self).OnReseted()
        self._sma = None
        self._highest = None
        self._lowest = None
        self._macd = None
        self._resistance_counter = 0.0
        self._support_counter = 0.0
        self._macd_down_counter = 0.0
        self._macd_up_counter = 0.0
        self._prev_macd_main = None
        self._prev_macd_signal = None
        self._planned_stop = None
        self._planned_take = None
        self._planned_side = None
        self._active_stop = None
        self._active_take = None
        self._active_side = None
        self._previous_high = None
        self._previous_low = None
        self._has_previous_candle = False

    def OnStarted(self, time):
        super(trading_lab_best_macd_strategy, self).OnStarted(time)

        self.__sma = SimpleMovingAverage()
        self.__sma.Length = self.ma_length
        self.__highest = Highest()
        self.__highest.Length = self.box_period
        self.__lowest = Lowest()
        self.__lowest.Length = self.box_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(new IIndicator[] { _sma, self.__highest, self.__lowest, _macd }, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return trading_lab_best_macd_strategy()
