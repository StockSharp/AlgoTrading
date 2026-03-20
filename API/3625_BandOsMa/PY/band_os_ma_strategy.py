import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergence
from StockSharp.Algo.Strategies import Strategy

class band_os_ma_strategy(Strategy):
    def __init__(self):
        super(band_os_ma_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._macd_fast_period = self.Param("MacdFastPeriod", 20)
        self._macd_slow_period = self.Param("MacdSlowPeriod", 50)
        self._macd_signal_period = self.Param("MacdSignalPeriod", 12)
        self._bollinger_period = self.Param("BollingerPeriod", 14)
        self._bollinger_deviation = self.Param("BollingerDeviation", 2.0)

        self._macd_history = []
        self._osma_history = []
        self._prev_osma = None
        self._prev_upper = None
        self._prev_lower = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def MacdFastPeriod(self):
        return self._macd_fast_period.Value

    @MacdFastPeriod.setter
    def MacdFastPeriod(self, value):
        self._macd_fast_period.Value = value

    @property
    def MacdSlowPeriod(self):
        return self._macd_slow_period.Value

    @MacdSlowPeriod.setter
    def MacdSlowPeriod(self, value):
        self._macd_slow_period.Value = value

    @property
    def MacdSignalPeriod(self):
        return self._macd_signal_period.Value

    @MacdSignalPeriod.setter
    def MacdSignalPeriod(self, value):
        self._macd_signal_period.Value = value

    @property
    def BollingerPeriod(self):
        return self._bollinger_period.Value

    @BollingerPeriod.setter
    def BollingerPeriod(self, value):
        self._bollinger_period.Value = value

    @property
    def BollingerDeviation(self):
        return self._bollinger_deviation.Value

    @BollingerDeviation.setter
    def BollingerDeviation(self, value):
        self._bollinger_deviation.Value = value

    def OnReseted(self):
        super(band_os_ma_strategy, self).OnReseted()
        self._macd_history = []
        self._osma_history = []
        self._prev_osma = None
        self._prev_upper = None
        self._prev_lower = None

    def OnStarted(self, time):
        super(band_os_ma_strategy, self).OnStarted(time)
        self._macd_history = []
        self._osma_history = []
        self._prev_osma = None
        self._prev_upper = None
        self._prev_lower = None

        macd = MovingAverageConvergenceDivergence()
        macd.ShortMa.Length = self.MacdFastPeriod
        macd.LongMa.Length = self.MacdSlowPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(macd, self._process_candle).Start()

    def _process_candle(self, candle, macd_value):
        if candle.State != CandleStates.Finished:
            return

        macd_val = float(macd_value)
        signal_period = self.MacdSignalPeriod
        bb_period = self.BollingerPeriod
        bb_dev = float(self.BollingerDeviation)

        # Compute signal line manually
        self._macd_history.append(macd_val)
        while len(self._macd_history) > signal_period:
            self._macd_history.pop(0)

        if len(self._macd_history) < signal_period:
            return

        signal = sum(self._macd_history) / signal_period
        osma = macd_val - signal

        # Compute Bollinger Bands on OsMA
        self._osma_history.append(osma)
        while len(self._osma_history) > bb_period:
            self._osma_history.pop(0)

        if len(self._osma_history) < bb_period:
            return

        mean = sum(self._osma_history) / len(self._osma_history)
        variance = sum((x - mean) ** 2 for x in self._osma_history) / len(self._osma_history)
        std_dev = math.sqrt(variance)
        upper = mean + bb_dev * std_dev
        lower = mean - bb_dev * std_dev

        if self._prev_osma is not None and self._prev_upper is not None and self._prev_lower is not None:
            # Buy: OsMA crosses below lower band
            if self._prev_osma > self._prev_lower and osma <= lower and self.Position <= 0:
                self.BuyMarket()
            # Sell: OsMA crosses above upper band
            elif self._prev_osma < self._prev_upper and osma >= upper and self.Position >= 0:
                self.SellMarket()

        self._prev_osma = osma
        self._prev_upper = upper
        self._prev_lower = lower

    def CreateClone(self):
        return band_os_ma_strategy()
