import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StandardDeviation, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class hvr_strategy(Strategy):

    def __init__(self):
        super(hvr_strategy, self).__init__()

        self._short_period = self.Param("ShortPeriod", 6) \
            .SetDisplay("Short HV Period", "Bars for short-term volatility", "Parameters")
        self._long_period = self.Param("LongPeriod", 100) \
            .SetDisplay("Long HV Period", "Bars for long-term volatility", "Parameters")
        self._ratio_threshold = self.Param("RatioThreshold", 1.0) \
            .SetDisplay("Ratio Threshold", "HVR level for trade direction", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe used for calculation", "General")

        self._short_sd = None
        self._long_sd = None
        self._prev_close = None

    @property
    def ShortPeriod(self):
        return self._short_period.Value

    @ShortPeriod.setter
    def ShortPeriod(self, value):
        self._short_period.Value = value

    @property
    def LongPeriod(self):
        return self._long_period.Value

    @LongPeriod.setter
    def LongPeriod(self, value):
        self._long_period.Value = value

    @property
    def RatioThreshold(self):
        return self._ratio_threshold.Value

    @RatioThreshold.setter
    def RatioThreshold(self, value):
        self._ratio_threshold.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(hvr_strategy, self).OnStarted(time)

        self._prev_close = None
        self._short_sd = StandardDeviation()
        self._short_sd.Length = self.ShortPeriod
        self._long_sd = StandardDeviation()
        self._long_sd.Length = self.LongPeriod

        self.SubscribeCandles(self.CandleType) \
            .Bind(self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)

        if self._prev_close is None or self._prev_close <= 0:
            self._prev_close = close
            return

        log_return = math.log(close / self._prev_close)
        self._prev_close = close

        t = candle.OpenTime

        short_result = self._short_sd.Process(DecimalIndicatorValue(self._short_sd, log_return, t, True))
        long_result = self._long_sd.Process(DecimalIndicatorValue(self._long_sd, log_return, t, True))

        if not short_result.IsFormed or not long_result.IsFormed:
            return

        short_val = float(short_result)
        long_val = float(long_result)

        if long_val == 0:
            return

        ratio = short_val / long_val
        threshold = float(self.RatioThreshold)

        if ratio > threshold and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif ratio < threshold and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

    def OnReseted(self):
        super(hvr_strategy, self).OnReseted()
        self._short_sd = None
        self._long_sd = None
        self._prev_close = None

    def CreateClone(self):
        return hvr_strategy()
