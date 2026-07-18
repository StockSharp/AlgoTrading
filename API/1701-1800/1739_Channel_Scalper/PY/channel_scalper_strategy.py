import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class channel_scalper_strategy(Strategy):
    def __init__(self):
        super(channel_scalper_strategy, self).__init__()
        self._stdev_period = self.Param("StdevPeriod", 14) \
            .SetDisplay("StdDev Period", "Standard deviation period", "General")
        self._multiplier = self.Param("Multiplier", 1.5) \
            .SetDisplay("Multiplier", "Channel width multiplier", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle Type", "General")
        self._up = 0.0
        self._down = 0.0
        self._direction = 0
        self._is_initialized = False

    @property
    def stdev_period(self):
        return self._stdev_period.Value

    @property
    def multiplier(self):
        return self._multiplier.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(channel_scalper_strategy, self).OnReseted()
        self._up = 0.0
        self._down = 0.0
        self._direction = 0
        self._is_initialized = False

    def OnStarted2(self, time):
        super(channel_scalper_strategy, self).OnStarted2(time)
        stdev = StandardDeviation()
        stdev.Length = self.stdev_period
        self.SubscribeCandles(self.candle_type).Bind(stdev, self.process_candle).Start()

    def process_candle(self, candle, stdev_value):
        if candle.State != CandleStates.Finished or float(stdev_value) <= 0:
            return

        middle = (float(candle.HighPrice) + float(candle.LowPrice)) / 2.0
        current_up = middle + float(self.multiplier) * float(stdev_value)
        current_down = middle - float(self.multiplier) * float(stdev_value)

        if not self._is_initialized:
            self._up = current_up
            self._down = current_down
            self._is_initialized = True
            return

        close = float(candle.ClosePrice)

        if self._direction <= 0 and close > self._up:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._direction = 1
        elif self._direction >= 0 and close < self._down:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._direction = -1

        if self._direction > 0:
            current_down = max(current_down, self._down)
        elif self._direction < 0:
            current_up = min(current_up, self._up)

        self._up = current_up
        self._down = current_down

    def CreateClone(self):
        return channel_scalper_strategy()
