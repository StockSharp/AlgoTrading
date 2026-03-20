import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class turbo_scaler_grid_strategy(Strategy):
    def __init__(self):
        super(turbo_scaler_grid_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(60) \
            .SetDisplay("Candle Type", "Timeframe for scalping", "General")
        self._fast_period = self.Param("FastPeriod", 14) \
            .SetDisplay("Candle Type", "Timeframe for scalping", "General")
        self._slow_period = self.Param("SlowPeriod", 34) \
            .SetDisplay("Candle Type", "Timeframe for scalping", "General")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("Candle Type", "Timeframe for scalping", "General")
        self._rsi_upper = self.Param("RsiUpper", 60) \
            .SetDisplay("Candle Type", "Timeframe for scalping", "General")
        self._rsi_lower = self.Param("RsiLower", 40) \
            .SetDisplay("Candle Type", "Timeframe for scalping", "General")

        self._fast_ema = None
        self._slow_ema = None
        self._rsi = None
        self._prev_fast = None
        self._prev_slow = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(turbo_scaler_grid_strategy, self).OnReseted()
        self._fast_ema = None
        self._slow_ema = None
        self._rsi = None
        self._prev_fast = None
        self._prev_slow = None

    def OnStarted(self, time):
        super(turbo_scaler_grid_strategy, self).OnStarted(time)

        self.__fast_ema = ExponentialMovingAverage()
        self.__fast_ema.Length = self.fast_period
        self.__slow_ema = ExponentialMovingAverage()
        self.__slow_ema.Length = self.slow_period
        self.__rsi = RelativeStrengthIndex()
        self.__rsi.Length = self.rsi_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__fast_ema, self.__slow_ema, self.__rsi, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return turbo_scaler_grid_strategy()
