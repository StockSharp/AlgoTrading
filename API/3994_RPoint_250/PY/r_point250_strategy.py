import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Unit, UnitTypes


class r_point250_strategy(Strategy):
    def __init__(self):
        super(r_point250_strategy, self).__init__()

        self._order_volume = self.Param("OrderVolume", 1) \
            .SetDisplay("Order Volume", "Base volume for market entries.", "Trading")
        self._take_profit_points = self.Param("TakeProfitPoints", 500) \
            .SetDisplay("Order Volume", "Base volume for market entries.", "Trading")
        self._stop_loss_points = self.Param("StopLossPoints", 999) \
            .SetDisplay("Order Volume", "Base volume for market entries.", "Trading")
        self._trailing_stop_points = self.Param("TrailingStopPoints", 0) \
            .SetDisplay("Order Volume", "Base volume for market entries.", "Trading")
        self._reverse_point = self.Param("ReversePoint", 250) \
            .SetDisplay("Order Volume", "Base volume for market entries.", "Trading")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1) \
            .SetDisplay("Order Volume", "Base volume for market entries.", "Trading")

        self._highest = None
        self._lowest = None
        self._last_high_level = 0.0
        self._last_low_level = 0.0
        self._executed_high_level = 0.0
        self._executed_low_level = 0.0
        self._last_signal_time = None
        self._price_step = 0.0
        self._trailing_distance = 0.0
        self._best_long_price = None
        self._best_short_price = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(r_point250_strategy, self).OnReseted()
        self._highest = None
        self._lowest = None
        self._last_high_level = 0.0
        self._last_low_level = 0.0
        self._executed_high_level = 0.0
        self._executed_low_level = 0.0
        self._last_signal_time = None
        self._price_step = 0.0
        self._trailing_distance = 0.0
        self._best_long_price = None
        self._best_short_price = None

    def OnStarted(self, time):
        super(r_point250_strategy, self).OnStarted(time)

        self.__highest = Highest()
        self.__highest.Length = Math.Max(1
        self.__lowest = Lowest()
        self.__lowest.Length = Math.Max(1

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__highest, self.__lowest, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return r_point250_strategy()
