import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class bb_strategy(Strategy):
    def __init__(self):
        super(bb_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._bollinger_period = self.Param("BollingerPeriod", 20)
        self._inner_deviation = self.Param("InnerDeviation", 2.0)
        self._outer_deviation = self.Param("OuterDeviation", 3.0)

        self._closes = []
        self._wait_direction = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def BollingerPeriod(self):
        return self._bollinger_period.Value

    @BollingerPeriod.setter
    def BollingerPeriod(self, value):
        self._bollinger_period.Value = value

    @property
    def InnerDeviation(self):
        return self._inner_deviation.Value

    @InnerDeviation.setter
    def InnerDeviation(self, value):
        self._inner_deviation.Value = value

    @property
    def OuterDeviation(self):
        return self._outer_deviation.Value

    @OuterDeviation.setter
    def OuterDeviation(self, value):
        self._outer_deviation.Value = value

    def OnReseted(self):
        super(bb_strategy, self).OnReseted()
        self._closes = []
        self._wait_direction = 0

    def OnStarted2(self, time):
        super(bb_strategy, self).OnStarted2(time)
        self._closes = []
        self._wait_direction = 0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

    def _compute_bands(self, period, deviation):
        if len(self._closes) < period:
            return None, None, None
        window = self._closes[-period:]
        mean = sum(window) / period
        variance = sum((x - mean) ** 2 for x in window) / period
        std = math.sqrt(variance)
        upper = mean + deviation * std
        lower = mean - deviation * std
        return mean, upper, lower

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        period = self.BollingerPeriod
        inner_dev = float(self.InnerDeviation)
        outer_dev = float(self.OuterDeviation)

        self._closes.append(close)
        while len(self._closes) > period + 10:
            self._closes.pop(0)

        if len(self._closes) < period:
            return

        _, inner_upper, inner_lower = self._compute_bands(period, inner_dev)
        _, outer_upper, outer_lower = self._compute_bands(period, outer_dev)

        if inner_upper is None or outer_upper is None:
            return

        # Detect outer band breakout
        if close > outer_upper:
            self._wait_direction = 1
        elif close < outer_lower:
            self._wait_direction = -1

        signal = 0

        # Check re-entry into inner band
        if self._wait_direction > 0 and close < inner_upper and close > inner_lower:
            signal = 1
            self._wait_direction = 0
        elif self._wait_direction < 0 and close > inner_lower and close < inner_upper:
            signal = -1
            self._wait_direction = 0

        if signal == 1 and self.Position <= 0:
            self.BuyMarket()
        elif signal == -1 and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return bb_strategy()
