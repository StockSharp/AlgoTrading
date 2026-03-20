import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import DecimalIndicatorValue, JurikMovingAverage
from StockSharp.Algo.Strategies import Strategy


class color_jfatl_digit_tm_plus_strategy(Strategy):
    def __init__(self):
        super(color_jfatl_digit_tm_plus_strategy, self).__init__()

        self._trade_volume = self.Param("TradeVolume", 1) \
            .SetDisplay("Volume", "Order volume", "Trading")
        self._stop_loss_points = self.Param("StopLossPoints", 1000) \
            .SetDisplay("Volume", "Order volume", "Trading")
        self._take_profit_points = self.Param("TakeProfitPoints", 2000) \
            .SetDisplay("Volume", "Order volume", "Trading")
        self._enable_buy_entries = self.Param("EnableBuyEntries", True) \
            .SetDisplay("Volume", "Order volume", "Trading")
        self._enable_sell_entries = self.Param("EnableSellEntries", True) \
            .SetDisplay("Volume", "Order volume", "Trading")
        self._enable_buy_exits = self.Param("EnableBuyExits", True) \
            .SetDisplay("Volume", "Order volume", "Trading")
        self._enable_sell_exits = self.Param("EnableSellExits", True) \
            .SetDisplay("Volume", "Order volume", "Trading")
        self._use_time_exit = self.Param("UseTimeExit", True) \
            .SetDisplay("Volume", "Order volume", "Trading")
        self._holding_minutes = self.Param("HoldingMinutes", 240) \
            .SetDisplay("Volume", "Order volume", "Trading")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Volume", "Order volume", "Trading")
        self._jma_length = self.Param("JmaLength", 5) \
            .SetDisplay("Volume", "Order volume", "Trading")
        self._applied_price = self.Param("AppliedPrice", AppliedPrices.Close) \
            .SetDisplay("Volume", "Order volume", "Trading")
        self._rounding_digits = self.Param("RoundingDigits", 2) \
            .SetDisplay("Volume", "Order volume", "Trading")
        self._signal_bar = self.Param("SignalBar", 1) \
            .SetDisplay("Volume", "Order volume", "Trading")

        self._indicator = None
        self._color_history = new()
        self._entry_price = None
        self._entry_time = None
        self._buffer_count = 0.0
        self._buffer_index = 0.0
        self._previous_line = None
        self._previous_color = None
        self._jma = new()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(color_jfatl_digit_tm_plus_strategy, self).OnReseted()
        self._indicator = None
        self._color_history = new()
        self._entry_price = None
        self._entry_time = None
        self._buffer_count = 0.0
        self._buffer_index = 0.0
        self._previous_line = None
        self._previous_color = None
        self._jma = new()

    def OnStarted(self, time):
        super(color_jfatl_digit_tm_plus_strategy, self).OnStarted(time)

        self.__indicator = ColorJfatlDigitIndicator()
        self.__indicator.AppliedPrices = self.applied_price
        self.__indicator.RoundingDigits = self.rounding_digits
        self.__indicator.JmaLength = self.jma_length

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
        return color_jfatl_digit_tm_plus_strategy()
