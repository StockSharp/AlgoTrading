import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import DecimalIndicatorValue, ExponentialMovingAverage, SimpleMovingAverage, SmoothedMovingAverage, TripleExponentialMovingAverage, WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Unit, UnitTypes


class x_didi_index_cloud_duplex_strategy(Strategy):
    def __init__(self):
        super(x_didi_index_cloud_duplex_strategy, self).__init__()

        self._long_candle_type = self.Param("LongCandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Long Candle Type", "Timeframe used for the long XDidi calculation", "General")
        self._long_fast_method = self.Param("LongFastMethod", SmoothingMethods.Sma) \
            .SetDisplay("Long Candle Type", "Timeframe used for the long XDidi calculation", "General")
        self._long_fast_length = self.Param("LongFastLength", 3) \
            .SetDisplay("Long Candle Type", "Timeframe used for the long XDidi calculation", "General")
        self._long_medium_method = self.Param("LongMediumMethod", SmoothingMethods.Sma) \
            .SetDisplay("Long Candle Type", "Timeframe used for the long XDidi calculation", "General")
        self._long_medium_length = self.Param("LongMediumLength", 8) \
            .SetDisplay("Long Candle Type", "Timeframe used for the long XDidi calculation", "General")
        self._long_slow_method = self.Param("LongSlowMethod", SmoothingMethods.Sma) \
            .SetDisplay("Long Candle Type", "Timeframe used for the long XDidi calculation", "General")
        self._long_slow_length = self.Param("LongSlowLength", 20) \
            .SetDisplay("Long Candle Type", "Timeframe used for the long XDidi calculation", "General")
        self._long_applied_price = self.Param("LongAppliedPrice", AppliedPrices.Close) \
            .SetDisplay("Long Candle Type", "Timeframe used for the long XDidi calculation", "General")
        self._long_enable_entry = self.Param("EnableLongEntries", True) \
            .SetDisplay("Long Candle Type", "Timeframe used for the long XDidi calculation", "General")
        self._long_enable_exit = self.Param("EnableLongExits", True) \
            .SetDisplay("Long Candle Type", "Timeframe used for the long XDidi calculation", "General")
        self._long_reverse = self.Param("LongReverse", False) \
            .SetDisplay("Long Candle Type", "Timeframe used for the long XDidi calculation", "General")
        self._long_signal_bar = self.Param("LongSignalBar", 0) \
            .SetDisplay("Long Candle Type", "Timeframe used for the long XDidi calculation", "General")
        self._short_candle_type = self.Param("ShortCandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Long Candle Type", "Timeframe used for the long XDidi calculation", "General")
        self._short_fast_method = self.Param("ShortFastMethod", SmoothingMethods.Sma) \
            .SetDisplay("Long Candle Type", "Timeframe used for the long XDidi calculation", "General")
        self._short_fast_length = self.Param("ShortFastLength", 3) \
            .SetDisplay("Long Candle Type", "Timeframe used for the long XDidi calculation", "General")
        self._short_medium_method = self.Param("ShortMediumMethod", SmoothingMethods.Sma) \
            .SetDisplay("Long Candle Type", "Timeframe used for the long XDidi calculation", "General")
        self._short_medium_length = self.Param("ShortMediumLength", 8) \
            .SetDisplay("Long Candle Type", "Timeframe used for the long XDidi calculation", "General")
        self._short_slow_method = self.Param("ShortSlowMethod", SmoothingMethods.Sma) \
            .SetDisplay("Long Candle Type", "Timeframe used for the long XDidi calculation", "General")
        self._short_slow_length = self.Param("ShortSlowLength", 20) \
            .SetDisplay("Long Candle Type", "Timeframe used for the long XDidi calculation", "General")
        self._short_applied_price = self.Param("ShortAppliedPrice", AppliedPrices.Close) \
            .SetDisplay("Long Candle Type", "Timeframe used for the long XDidi calculation", "General")
        self._short_enable_entry = self.Param("EnableShortEntries", True) \
            .SetDisplay("Long Candle Type", "Timeframe used for the long XDidi calculation", "General")
        self._short_enable_exit = self.Param("EnableShortExits", True) \
            .SetDisplay("Long Candle Type", "Timeframe used for the long XDidi calculation", "General")
        self._short_reverse = self.Param("ShortReverse", False) \
            .SetDisplay("Long Candle Type", "Timeframe used for the long XDidi calculation", "General")
        self._short_signal_bar = self.Param("ShortSignalBar", 0) \
            .SetDisplay("Long Candle Type", "Timeframe used for the long XDidi calculation", "General")
        self._stop_loss_points = self.Param("StopLossPoints", 1000) \
            .SetDisplay("Long Candle Type", "Timeframe used for the long XDidi calculation", "General")
        self._take_profit_points = self.Param("TakeProfitPoints", 2000) \
            .SetDisplay("Long Candle Type", "Timeframe used for the long XDidi calculation", "General")

        self._long_fast_ma = null!
        self._long_medium_ma = null!
        self._long_slow_ma = null!
        self._short_fast_ma = null!
        self._short_medium_ma = null!
        self._short_slow_ma = null!

    def OnReseted(self):
        super(x_didi_index_cloud_duplex_strategy, self).OnReseted()
        self._long_fast_ma = null!
        self._long_medium_ma = null!
        self._long_slow_ma = null!
        self._short_fast_ma = null!
        self._short_medium_ma = null!
        self._short_slow_ma = null!

    def OnStarted(self, time):
        super(x_didi_index_cloud_duplex_strategy, self).OnStarted(time)


        long_subscription = self.SubscribeCandles(Longself.candle_type)
        long_subscription.Bind(self._process_candle).Start()

        subscription = self.SubscribeCandles(Shortself.candle_type)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return x_didi_index_cloud_duplex_strategy()
