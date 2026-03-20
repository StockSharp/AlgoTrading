import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, MovingAverageConvergenceDivergence
from StockSharp.Algo.Strategies import Strategy


class macd_fixed_psar_strategy(Strategy):
    def __init__(self):
        super(macd_fixed_psar_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(60) \
            .SetDisplay("Candle Type", "Timeframe for MACD calculations", "General")
        self._fast_period = self.Param("FastPeriod", 20) \
            .SetDisplay("Candle Type", "Timeframe for MACD calculations", "General")
        self._slow_period = self.Param("SlowPeriod", 50) \
            .SetDisplay("Candle Type", "Timeframe for MACD calculations", "General")
        self._signal_period = self.Param("SignalPeriod", 12) \
            .SetDisplay("Candle Type", "Timeframe for MACD calculations", "General")
        self._trend_period = self.Param("TrendPeriod", 60) \
            .SetDisplay("Candle Type", "Timeframe for MACD calculations", "General")

        self._macd = None
        self._trend_ema = None
        self._macd_history = new()
        self._prev_histogram = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(macd_fixed_psar_strategy, self).OnReseted()
        self._macd = None
        self._trend_ema = None
        self._macd_history = new()
        self._prev_histogram = None

    def OnStarted(self, time):
        super(macd_fixed_psar_strategy, self).OnStarted(time)

        self.__trend_ema = ExponentialMovingAverage()
        self.__trend_ema.Length = self.trend_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(_macd, self.__trend_ema, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return macd_fixed_psar_strategy()
