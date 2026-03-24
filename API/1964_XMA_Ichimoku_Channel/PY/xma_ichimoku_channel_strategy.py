import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from collections import deque
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import DecimalIndicatorValue, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class xma_ichimoku_channel_strategy(Strategy):

    def __init__(self):
        super(xma_ichimoku_channel_strategy, self).__init__()

        self._up_period = self.Param("UpPeriod", 3) \
            .SetDisplay("Up Period", "Lookback for high prices", "Channel")
        self._down_period = self.Param("DownPeriod", 3) \
            .SetDisplay("Down Period", "Lookback for low prices", "Channel")
        self._ma_length = self.Param("MaLength", 100) \
            .SetDisplay("MA Length", "Smoothing length", "Channel")
        self._up_percent = self.Param("UpPercent", 1.0) \
            .SetDisplay("Up Percent", "Upper band offset in %", "Channel")
        self._down_percent = self.Param("DownPercent", 1.0) \
            .SetDisplay("Down Percent", "Lower band offset in %", "Channel")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._highs = deque()
        self._lows = deque()
        self._sma = None
        self._is_initialized = False
        self._prev_upper = 0.0
        self._prev_lower = 0.0
        self._prev_close = 0.0

    @property
    def UpPeriod(self):
        return self._up_period.Value

    @UpPeriod.setter
    def UpPeriod(self, value):
        self._up_period.Value = value

    @property
    def DownPeriod(self):
        return self._down_period.Value

    @DownPeriod.setter
    def DownPeriod(self, value):
        self._down_period.Value = value

    @property
    def MaLength(self):
        return self._ma_length.Value

    @MaLength.setter
    def MaLength(self, value):
        self._ma_length.Value = value

    @property
    def UpPercent(self):
        return self._up_percent.Value

    @UpPercent.setter
    def UpPercent(self, value):
        self._up_percent.Value = value

    @property
    def DownPercent(self):
        return self._down_percent.Value

    @DownPercent.setter
    def DownPercent(self, value):
        self._down_percent.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(xma_ichimoku_channel_strategy, self).OnStarted(time)

        self._sma = SimpleMovingAverage()
        self._sma.Length = self.MaLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._sma)

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._highs.append(float(candle.HighPrice))
        if len(self._highs) > self.UpPeriod:
            self._highs.popleft()

        self._lows.append(float(candle.LowPrice))
        if len(self._lows) > self.DownPeriod:
            self._lows.popleft()

        if len(self._highs) < self.UpPeriod or len(self._lows) < self.DownPeriod:
            return

        highest = max(self._highs)
        lowest = min(self._lows)
        mid_value = (highest + lowest) / 2.0

        mi = DecimalIndicatorValue(self._sma, mid_value, candle.OpenTime)
        mi.IsFinal = True
        result = self._sma.Process(mi)
        middle = float(result)

        if not self._sma.IsFormed:
            return

        upper = middle * (1.0 + float(self.UpPercent) / 100.0)
        lower = middle * (1.0 - float(self.DownPercent) / 100.0)

        if not self._is_initialized:
            self._prev_upper = upper
            self._prev_lower = lower
            self._prev_close = float(candle.ClosePrice)
            self._is_initialized = True
            return

        close = float(candle.ClosePrice)

        if self._prev_close > self._prev_upper and close <= upper and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif self._prev_close < self._prev_lower and close >= lower and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

        self._prev_upper = upper
        self._prev_lower = lower
        self._prev_close = close

    def OnReseted(self):
        super(xma_ichimoku_channel_strategy, self).OnReseted()
        self._highs = deque()
        self._lows = deque()
        if self._sma is not None:
            self._sma.Length = self.MaLength
            self._sma.Reset()
        self._is_initialized = False
        self._prev_upper = 0.0
        self._prev_lower = 0.0
        self._prev_close = 0.0

    def CreateClone(self):
        return xma_ichimoku_channel_strategy()
