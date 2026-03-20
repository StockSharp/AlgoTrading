import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Unit, UnitTypes


class macd_zero_filtered_cross_strategy(Strategy):
    def __init__(self):
        super(macd_zero_filtered_cross_strategy, self).__init__()

        self._fast_period = self.Param("FastPeriod", 12) \
            .SetDisplay("Fast Period", "Short EMA period for MACD", "MACD")
        self._slow_period = self.Param("SlowPeriod", 26) \
            .SetDisplay("Fast Period", "Short EMA period for MACD", "MACD")
        self._signal_period = self.Param("SignalPeriod", 9) \
            .SetDisplay("Fast Period", "Short EMA period for MACD", "MACD")
        self._take_profit_points = self.Param("TakeProfitPoints", 300) \
            .SetDisplay("Fast Period", "Short EMA period for MACD", "MACD")
        self._lot_volume = self.Param("LotVolume", 1) \
            .SetDisplay("Fast Period", "Short EMA period for MACD", "MACD")
        self._minimum_balance_per_volume = self.Param("MinimumBalancePerVolume", 1000) \
            .SetDisplay("Fast Period", "Short EMA period for MACD", "MACD")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1) \
            .SetDisplay("Fast Period", "Short EMA period for MACD", "MACD")

        self._macd = null!
        self._previous_macd = None
        self._previous_signal = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(macd_zero_filtered_cross_strategy, self).OnReseted()
        self._macd = null!
        self._previous_macd = None
        self._previous_signal = None

    def OnStarted(self, time):
        super(macd_zero_filtered_cross_strategy, self).OnStarted(time)


        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(_macd, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return macd_zero_filtered_cross_strategy()
