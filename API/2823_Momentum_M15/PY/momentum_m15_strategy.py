import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import DecimalIndicatorValue, ExponentialMovingAverage, Momentum, SimpleMovingAverage, SmoothedMovingAverage, WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class momentum_m15_strategy(Strategy):
    def __init__(self):
        super(momentum_m15_strategy, self).__init__()

        self._volume_param = self.Param("TradeVolume", 0.1) \
            .SetDisplay("Trade Volume", "Default order volume", "Trading")
        self._candle_type_param = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Trade Volume", "Default order volume", "Trading")
        self._ma_period_param = self.Param("MaPeriod", 26) \
            .SetDisplay("Trade Volume", "Default order volume", "Trading")
        self._ma_shift_param = self.Param("MaShift", 8) \
            .SetDisplay("Trade Volume", "Default order volume", "Trading")
        self._ma_method_param = self.Param("MaMethod", MovingAverageMethods.Smoothed) \
            .SetDisplay("Trade Volume", "Default order volume", "Trading")
        self._ma_price_param = self.Param("MaPrice", CandlePrices.Low) \
            .SetDisplay("Trade Volume", "Default order volume", "Trading")
        self._momentum_period_param = self.Param("MomentumPeriod", 23) \
            .SetDisplay("Trade Volume", "Default order volume", "Trading")
        self._momentum_price_param = self.Param("MomentumPrice", CandlePrices.Open) \
            .SetDisplay("Trade Volume", "Default order volume", "Trading")
        self._momentum_threshold_param = self.Param("MomentumThreshold", 100) \
            .SetDisplay("Trade Volume", "Default order volume", "Trading")
        self._momentum_shift_param = self.Param("MomentumShift", -0.2) \
            .SetDisplay("Trade Volume", "Default order volume", "Trading")
        self._momentum_open_length_param = self.Param("MomentumOpenLength", 6) \
            .SetDisplay("Trade Volume", "Default order volume", "Trading")
        self._momentum_close_length_param = self.Param("MomentumCloseLength", 10) \
            .SetDisplay("Trade Volume", "Default order volume", "Trading")
        self._gap_level_param = self.Param("GapLevel", 30) \
            .SetDisplay("Trade Volume", "Default order volume", "Trading")
        self._gap_timeout_param = self.Param("GapTimeout", 100) \
            .SetDisplay("Trade Volume", "Default order volume", "Trading")
        self._trailing_stop_param = self.Param("TrailingStop", 0) \
            .SetDisplay("Trade Volume", "Default order volume", "Trading")

        self._ma = null!
        self._momentum = null!
        self._ma_history = new()
        self._momentum_history = new()
        self._previous_close = None
        self._long_trailing_stop = None
        self._short_trailing_stop = None
        self._gap_timer = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(momentum_m15_strategy, self).OnReseted()
        self._ma = null!
        self._momentum = null!
        self._ma_history = new()
        self._momentum_history = new()
        self._previous_close = None
        self._long_trailing_stop = None
        self._short_trailing_stop = None
        self._gap_timer = 0.0

    def OnStarted(self, time):
        super(momentum_m15_strategy, self).OnStarted(time)

        self.__momentum = Momentum()
        self.__momentum.Length = self.momentum_period

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
        return momentum_m15_strategy()
