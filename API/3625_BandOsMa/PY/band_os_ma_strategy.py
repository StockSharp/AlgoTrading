import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, DecimalIndicatorValue, MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy


class band_os_ma_strategy(Strategy):
    def __init__(self):
        super(band_os_ma_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(60) \
            .SetDisplay("Candle Type", "Primary timeframe", "General")
        self._macd_fast_period = self.Param("MacdFastPeriod", 20) \
            .SetDisplay("Candle Type", "Primary timeframe", "General")
        self._macd_slow_period = self.Param("MacdSlowPeriod", 50) \
            .SetDisplay("Candle Type", "Primary timeframe", "General")
        self._macd_signal_period = self.Param("MacdSignalPeriod", 12) \
            .SetDisplay("Candle Type", "Primary timeframe", "General")
        self._bollinger_period = self.Param("BollingerPeriod", 14) \
            .SetDisplay("Candle Type", "Primary timeframe", "General")
        self._bollinger_deviation = self.Param("BollingerDeviation", 2) \
            .SetDisplay("Candle Type", "Primary timeframe", "General")

        self._prev_osma = 0.0
        self._prev_upper = 0.0
        self._prev_lower = 0.0
        self._has_prev = False
        self._bollinger = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(band_os_ma_strategy, self).OnReseted()
        self._prev_osma = 0.0
        self._prev_upper = 0.0
        self._prev_lower = 0.0
        self._has_prev = False
        self._bollinger = None

    def OnStarted(self, time):
        super(band_os_ma_strategy, self).OnStarted(time)


        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return band_os_ma_strategy()
