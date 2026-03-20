import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage as EMA, SimpleMovingAverage as SMA, SmoothedMovingAverage, StandardDeviation, WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Sides


class corrected_average_channel_strategy(Strategy):
    def __init__(self):
        super(corrected_average_channel_strategy, self).__init__()

        self._order_volume = self.Param("OrderVolume", 0.1) \
            .SetDisplay("Order Volume", "Market order size used for entries", "Trading")
        self._take_profit_points = self.Param("TakeProfitPoints", 60) \
            .SetDisplay("Order Volume", "Market order size used for entries", "Trading")
        self._stop_loss_points = self.Param("StopLossPoints", 40) \
            .SetDisplay("Order Volume", "Market order size used for entries", "Trading")
        self._trailing_points = self.Param("TrailingPoints", 0) \
            .SetDisplay("Order Volume", "Market order size used for entries", "Trading")
        self._trailing_step_points = self.Param("TrailingStepPoints", 0) \
            .SetDisplay("Order Volume", "Market order size used for entries", "Trading")
        self._ma_period = self.Param("MaPeriod", 35) \
            .SetDisplay("Order Volume", "Market order size used for entries", "Trading")
        self._ma_type = self.Param("MaTypesOption", MaTypes.Sma) \
            .SetDisplay("Order Volume", "Market order size used for entries", "Trading")
        self._sigma_buy_points = self.Param("SigmaBuyPoints", 5) \
            .SetDisplay("Order Volume", "Market order size used for entries", "Trading")
        self._sigma_sell_points = self.Param("SigmaSellPoints", 5) \
            .SetDisplay("Order Volume", "Market order size used for entries", "Trading")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1) \
            .SetDisplay("Order Volume", "Market order size used for entries", "Trading")

        self._ma = None
        self._std = None
        self._price_step = 0.0
        self._sigma_buy_offset = 0.0
        self._sigma_sell_offset = 0.0
        self._stop_loss_distance = 0.0
        self._take_profit_distance = 0.0
        self._trailing_distance = 0.0
        self._trailing_step_distance = 0.0
        self._previous_corrected = None
        self._previous_close = None
        self._entry_price = None
        self._stop_loss_price = None
        self._take_profit_price = None
        self._long_trailing_stop = None
        self._short_trailing_stop = None
        self._previous_position = 0.0
        self._last_trade_price = None
        self._last_trade_side = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(corrected_average_channel_strategy, self).OnReseted()
        self._ma = None
        self._std = None
        self._price_step = 0.0
        self._sigma_buy_offset = 0.0
        self._sigma_sell_offset = 0.0
        self._stop_loss_distance = 0.0
        self._take_profit_distance = 0.0
        self._trailing_distance = 0.0
        self._trailing_step_distance = 0.0
        self._previous_corrected = None
        self._previous_close = None
        self._entry_price = None
        self._stop_loss_price = None
        self._take_profit_price = None
        self._long_trailing_stop = None
        self._short_trailing_stop = None
        self._previous_position = 0.0
        self._last_trade_price = None
        self._last_trade_side = None

    def OnStarted(self, time):
        super(corrected_average_channel_strategy, self).OnStarted(time)

        self.__std = StandardDeviation()
        self.__std.Length = self.ma_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(_ma, self.__std, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return corrected_average_channel_strategy()
