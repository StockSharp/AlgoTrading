import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class bounce_strength_index_strategy(Strategy):
    def __init__(self):
        super(bounce_strength_index_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._range_period = self.Param("RangePeriod", 10) \
            .SetDisplay("Range Period", "Period for highest and lowest search", "Indicator")
        self._sma_period = self.Param("SmaPeriod", 10) \
            .SetDisplay("SMA Period", "SMA period for trend filter", "Indicator")
        self._highs = []
        self._lows = []
        self._prev_bsi = None
        self._prev_rising = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def range_period(self):
        return self._range_period.Value

    @property
    def sma_period(self):
        return self._sma_period.Value

    def OnReseted(self):
        super(bounce_strength_index_strategy, self).OnReseted()
        self._highs = []
        self._lows = []
        self._prev_bsi = None
        self._prev_rising = None

    def OnStarted(self, time):
        super(bounce_strength_index_strategy, self).OnStarted(time)

        sma = ExponentialMovingAverage()
        sma.Length = self.sma_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, sma_value):
        if candle.State != CandleStates.Finished:
            return

        rp = int(self.range_period)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)

        self._highs.append(high)
        self._lows.append(low)
        if len(self._highs) > rp:
            self._highs.pop(0)
        if len(self._lows) > rp:
            self._lows.pop(0)

        if len(self._highs) < 3:
            return

        h = max(self._highs)
        l = min(self._lows)
        rng = h - l
        if rng <= 0:
            return

        bsi = (close - l) / rng * 100.0

        if self._prev_bsi is not None:
            rising = bsi > self._prev_bsi

            if rising and self._prev_rising != True and self.Position <= 0:
                self.BuyMarket()
            elif not rising and self._prev_rising != False and self.Position >= 0:
                self.SellMarket()

            self._prev_rising = rising

        self._prev_bsi = bsi

    def CreateClone(self):
        return bounce_strength_index_strategy()
