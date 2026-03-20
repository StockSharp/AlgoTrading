import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import DecimalIndicatorValue, ExponentialMovingAverage, JurikMovingAverage, SimpleMovingAverage, SmoothedMovingAverage, WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class x_fatl_x_satl_cloud_duplex_strategy(Strategy):
    def __init__(self):
        super(x_fatl_x_satl_cloud_duplex_strategy, self).__init__()

        self._long_volume = self.Param("LongVolume", 1) \
            .SetDisplay("Long Volume", "Order volume for long entries", "Trading")
        self._short_volume = self.Param("ShortVolume", 1) \
            .SetDisplay("Long Volume", "Order volume for long entries", "Trading")
        self._long_allow_open = self.Param("LongAllowOpen", True) \
            .SetDisplay("Long Volume", "Order volume for long entries", "Trading")
        self._long_allow_close = self.Param("LongAllowClose", True) \
            .SetDisplay("Long Volume", "Order volume for long entries", "Trading")
        self._short_allow_open = self.Param("ShortAllowOpen", True) \
            .SetDisplay("Long Volume", "Order volume for long entries", "Trading")
        self._short_allow_close = self.Param("ShortAllowClose", True) \
            .SetDisplay("Long Volume", "Order volume for long entries", "Trading")
        self._long_signal_bar = self.Param("LongSignalBar", 1) \
            .SetDisplay("Long Volume", "Order volume for long entries", "Trading")
        self._short_signal_bar = self.Param("ShortSignalBar", 1) \
            .SetDisplay("Long Volume", "Order volume for long entries", "Trading")
        self._long_candle_type = self.Param("LongCandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Long Volume", "Order volume for long entries", "Trading")
        self._short_candle_type = self.Param("ShortCandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Long Volume", "Order volume for long entries", "Trading")
        self._long_method1 = self.Param("LongMethod1", XmaMethods.Jurik) \
            .SetDisplay("Long Volume", "Order volume for long entries", "Trading")
        self._long_length1 = self.Param("LongLength1", 3) \
            .SetDisplay("Long Volume", "Order volume for long entries", "Trading")
        self._long_phase1 = self.Param("LongPhase1", 15) \
            .SetDisplay("Long Volume", "Order volume for long entries", "Trading")
        self._long_method2 = self.Param("LongMethod2", XmaMethods.Jurik) \
            .SetDisplay("Long Volume", "Order volume for long entries", "Trading")
        self._long_length2 = self.Param("LongLength2", 5) \
            .SetDisplay("Long Volume", "Order volume for long entries", "Trading")
        self._long_phase2 = self.Param("LongPhase2", 15) \
            .SetDisplay("Long Volume", "Order volume for long entries", "Trading")
        self._long_price_type = self.Param("LongAppliedPrice", AppliedPrices.Close) \
            .SetDisplay("Long Volume", "Order volume for long entries", "Trading")
        self._short_method1 = self.Param("ShortMethod1", XmaMethods.Jurik) \
            .SetDisplay("Long Volume", "Order volume for long entries", "Trading")
        self._short_length1 = self.Param("ShortLength1", 3) \
            .SetDisplay("Long Volume", "Order volume for long entries", "Trading")
        self._short_phase1 = self.Param("ShortPhase1", 15) \
            .SetDisplay("Long Volume", "Order volume for long entries", "Trading")
        self._short_method2 = self.Param("ShortMethod2", XmaMethods.Jurik) \
            .SetDisplay("Long Volume", "Order volume for long entries", "Trading")
        self._short_length2 = self.Param("ShortLength2", 5) \
            .SetDisplay("Long Volume", "Order volume for long entries", "Trading")
        self._short_phase2 = self.Param("ShortPhase2", 15) \
            .SetDisplay("Long Volume", "Order volume for long entries", "Trading")
        self._short_price_type = self.Param("ShortAppliedPrice", AppliedPrices.Close) \
            .SetDisplay("Long Volume", "Order volume for long entries", "Trading")
        self._long_stop_loss = self.Param("LongStopLoss", 0) \
            .SetDisplay("Long Volume", "Order volume for long entries", "Trading")
        self._long_take_profit = self.Param("LongTakeProfit", 0) \
            .SetDisplay("Long Volume", "Order volume for long entries", "Trading")
        self._short_stop_loss = self.Param("ShortStopLoss", 0) \
            .SetDisplay("Long Volume", "Order volume for long entries", "Trading")
        self._short_take_profit = self.Param("ShortTakeProfit", 0) \
            .SetDisplay("Long Volume", "Order volume for long entries", "Trading")

        self._long_indicator = null!
        self._short_indicator = null!
        self._long_history = new()
        self._short_history = new()
        self._long_entry_price = None
        self._short_entry_price = None
        self._fast_smoother = None
        self._slow_smoother = None
        self._applied_price = None
        self._buffer_index = 0.0
        self._buffer_count = 0.0

    def OnReseted(self):
        super(x_fatl_x_satl_cloud_duplex_strategy, self).OnReseted()
        self._long_indicator = null!
        self._short_indicator = null!
        self._long_history = new()
        self._short_history = new()
        self._long_entry_price = None
        self._short_entry_price = None
        self._fast_smoother = None
        self._slow_smoother = None
        self._applied_price = None
        self._buffer_index = 0.0
        self._buffer_count = 0.0

    def OnStarted(self, time):
        super(x_fatl_x_satl_cloud_duplex_strategy, self).OnStarted(time)


        long_subscription = self.SubscribeCandles(Longself.candle_type)
        long_subscription.BindEx(_longIndicator, self._process_candle).Start()

        short_subscription = self.SubscribeCandles(Shortself.candle_type)
        short_subscription.BindEx(_shortIndicator, self._process_candle_1).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return x_fatl_x_satl_cloud_duplex_strategy()
