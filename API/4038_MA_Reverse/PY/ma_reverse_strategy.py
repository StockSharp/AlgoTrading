import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class ma_reverse_strategy(Strategy):
    """
    MA reversal: counts consecutive closes on one side of SMA.
    After streak threshold, opens a reversal trade.
    """

    def __init__(self):
        super(ma_reverse_strategy, self).__init__()
        self._sma_period = self.Param("SmaPeriod", 14).SetDisplay("SMA Period", "SMA period", "Indicators")
        self._streak_threshold = self.Param("StreakThreshold", 3).SetDisplay("Streak", "Consecutive closes needed", "Logic")
        self._min_deviation = self.Param("MinDeviation", 0.0001).SetDisplay("Min Deviation", "Min distance from SMA", "Logic")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Timeframe", "General")
        self._streak = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ma_reverse_strategy, self).OnReseted()
        self._streak = 0

    def OnStarted(self, time):
        super(ma_reverse_strategy, self).OnStarted(time)
        sma = SimpleMovingAverage()
        sma.Length = self._sma_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, sma_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        close = float(candle.ClosePrice)
        sma = float(sma_val)
        deviation = close - sma
        if deviation == 0:
            self._streak = 0
            return
        if deviation > 0:
            if self._streak < 0:
                self._streak = 0
            self._streak += 1
            if self._streak >= self._streak_threshold.Value and deviation > self._min_deviation.Value:
                if self.Position >= 0:
                    self.SellMarket()
                    self._streak = 0
        else:
            if self._streak > 0:
                self._streak = 0
            self._streak -= 1
            if -self._streak >= self._streak_threshold.Value and -deviation > self._min_deviation.Value:
                if self.Position <= 0:
                    self.BuyMarket()
                    self._streak = 0

    def CreateClone(self):
        return ma_reverse_strategy()
