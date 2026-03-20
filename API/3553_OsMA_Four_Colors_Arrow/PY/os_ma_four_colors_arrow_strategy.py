import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergence
from StockSharp.Algo.Strategies import Strategy


class os_ma_four_colors_arrow_strategy(Strategy):
    def __init__(self):
        super(os_ma_four_colors_arrow_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(60) \
            .SetDisplay("Candle Type", "Timeframe used for signal generation", "General")
        self._fast_period = self.Param("FastPeriod", 20) \
            .SetDisplay("Candle Type", "Timeframe used for signal generation", "General")
        self._slow_period = self.Param("SlowPeriod", 50) \
            .SetDisplay("Candle Type", "Timeframe used for signal generation", "General")
        self._signal_period = self.Param("SignalPeriod", 12) \
            .SetDisplay("Candle Type", "Timeframe used for signal generation", "General")

        self._macd = None
        self._macd_history = new()
        self._prev_histogram = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(os_ma_four_colors_arrow_strategy, self).OnReseted()
        self._macd = None
        self._macd_history = new()
        self._prev_histogram = None

    def OnStarted(self, time):
        super(os_ma_four_colors_arrow_strategy, self).OnStarted(time)


        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(_macd, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return os_ma_four_colors_arrow_strategy()
