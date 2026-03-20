import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergence
from StockSharp.Algo.Strategies import Strategy


class fatl_macd_strategy(Strategy):

    def __init__(self):
        super(fatl_macd_strategy, self).__init__()

        self._fast_length = self.Param("FastLength", 12) \
            .SetDisplay("Fast EMA", "Period of the fast moving average", "MACD")
        self._slow_length = self.Param("SlowLength", 26) \
            .SetDisplay("Slow EMA", "Period of the slow moving average", "MACD")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles for processing", "General")

        self._prev1 = 0.0
        self._prev2 = 0.0
        self._is_initialized = False

    @property
    def FastLength(self):
        return self._fast_length.Value

    @FastLength.setter
    def FastLength(self, value):
        self._fast_length.Value = value

    @property
    def SlowLength(self):
        return self._slow_length.Value

    @SlowLength.setter
    def SlowLength(self, value):
        self._slow_length.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(fatl_macd_strategy, self).OnStarted(time)

        macd = MovingAverageConvergenceDivergence()
        macd.ShortMa.Length = self.FastLength
        macd.LongMa.Length = self.SlowLength

        self.SubscribeCandles(self.CandleType) \
            .Bind(macd, self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle, macd_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        val = float(macd_value)

        if not self._is_initialized:
            self._prev2 = val
            self._prev1 = val
            self._is_initialized = True
            return

        if self._prev1 < self._prev2:
            if self.Position < 0:
                self.BuyMarket()
            if val > self._prev1 and self.Position <= 0:
                self.BuyMarket()
        elif self._prev1 > self._prev2:
            if self.Position > 0:
                self.SellMarket()
            if val < self._prev1 and self.Position >= 0:
                self.SellMarket()

        self._prev2 = self._prev1
        self._prev1 = val

    def OnReseted(self):
        super(fatl_macd_strategy, self).OnReseted()
        self._prev1 = 0.0
        self._prev2 = 0.0
        self._is_initialized = False

    def CreateClone(self):
        return fatl_macd_strategy()
