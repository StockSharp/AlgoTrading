import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class open_tiks_strategy(Strategy):
    def __init__(self):
        super(open_tiks_strategy, self).__init__()

        self._order_volume = self.Param("OrderVolume", 0.1) \
            .SetDisplay("Order Volume", "Volume of each market entry in lots.", "Trading")
        self._stop_loss_points = self.Param("StopLossPoints", 0) \
            .SetDisplay("Order Volume", "Volume of each market entry in lots.", "Trading")
        self._trailing_stop_points = self.Param("TrailingStopPoints", 30) \
            .SetDisplay("Order Volume", "Volume of each market entry in lots.", "Trading")
        self._max_orders = self.Param("MaxOrders", 1) \
            .SetDisplay("Order Volume", "Volume of each market entry in lots.", "Trading")
        self._use_partial_close = self.Param("UsePartialClose", True) \
            .SetDisplay("Order Volume", "Volume of each market entry in lots.", "Trading")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Order Volume", "Volume of each market entry in lots.", "Trading")

        self._price_step = 0.0
        self._volume_step = 0.0
        self._min_volume_limit = 0.0
        self._max_volume_limit = 0.0
        self._high1 = None
        self._high2 = None
        self._high3 = None
        self._open1 = None
        self._open2 = None
        self._open3 = None
        self._long_entry_price = None
        self._short_entry_price = None
        self._long_trailing_stop = None
        self._short_trailing_stop = None
        self._dummy_sma = None
        self._previous_position = 0.0
        self._last_trade_price = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(open_tiks_strategy, self).OnReseted()
        self._price_step = 0.0
        self._volume_step = 0.0
        self._min_volume_limit = 0.0
        self._max_volume_limit = 0.0
        self._high1 = None
        self._high2 = None
        self._high3 = None
        self._open1 = None
        self._open2 = None
        self._open3 = None
        self._long_entry_price = None
        self._short_entry_price = None
        self._long_trailing_stop = None
        self._short_trailing_stop = None
        self._dummy_sma = None
        self._previous_position = 0.0
        self._last_trade_price = None

    def OnStarted(self, time):
        super(open_tiks_strategy, self).OnStarted(time)

        self.__dummy_sma = SimpleMovingAverage()
        self.__dummy_sma.Length = 2

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__dummy_sma, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return open_tiks_strategy()
