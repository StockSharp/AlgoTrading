import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class reverse_strategy(Strategy):
    def __init__(self):
        super(reverse_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(30) \
            .SetDisplay("Candle Type", "Timeframe for signals", "General")
        self._bollinger_period = self.Param("BollingerPeriod", 20) \
            .SetDisplay("Candle Type", "Timeframe for signals", "General")
        self._bollinger_width = self.Param("BollingerWidth", 1) \
            .SetDisplay("Candle Type", "Timeframe for signals", "General")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("Candle Type", "Timeframe for signals", "General")
        self._rsi_overbought = self.Param("RsiOverbought", 70) \
            .SetDisplay("Candle Type", "Timeframe for signals", "General")
        self._rsi_oversold = self.Param("RsiOversold", 30) \
            .SetDisplay("Candle Type", "Timeframe for signals", "General")

        self._ema = None
        self._rsi = None
        self._prev_close = 0.0
        self._prev_rsi = 0.0
        self._prev_lower = 0.0
        self._prev_upper = 0.0
        self._has_prev = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(reverse_strategy, self).OnReseted()
        self._ema = None
        self._rsi = None
        self._prev_close = 0.0
        self._prev_rsi = 0.0
        self._prev_lower = 0.0
        self._prev_upper = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(reverse_strategy, self).OnStarted(time)

        self.__ema = ExponentialMovingAverage()
        self.__ema.Length = self.bollinger_period
        self.__rsi = RelativeStrengthIndex()
        self.__rsi.Length = self.rsi_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__ema, self.__rsi, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return reverse_strategy()
