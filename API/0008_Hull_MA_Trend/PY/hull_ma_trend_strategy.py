import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import HullMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class hull_ma_trend_strategy(Strategy):

    def __init__(self):
        super(hull_ma_trend_strategy, self).__init__()

        self._hma_period = self.Param("HmaPeriod", 500) \
            .SetDisplay("HMA Period", "Period for Hull Moving Average", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "Period for Average True Range (stop-loss)", "Risk parameters")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0) \
            .SetDisplay("ATR Multiplier", "Multiplier for ATR to determine stop-loss distance", "Risk parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_hma_value = 0.0

    @property
    def HmaPeriod(self):
        return self._hma_period.Value

    @HmaPeriod.setter
    def HmaPeriod(self, value):
        self._hma_period.Value = value

    @property
    def AtrPeriod(self):
        return self._atr_period.Value

    @AtrPeriod.setter
    def AtrPeriod(self, value):
        self._atr_period.Value = value

    @property
    def AtrMultiplier(self):
        return self._atr_multiplier.Value

    @AtrMultiplier.setter
    def AtrMultiplier(self, value):
        self._atr_multiplier.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(hull_ma_trend_strategy, self).OnStarted(time)

        self._prev_hma_value = 0.0

        hma = HullMovingAverage()
        hma.Length = self.HmaPeriod
        atr = AverageTrueRange()
        atr.Length = self.AtrPeriod

        self.SubscribeCandles(self.CandleType) \
            .Bind(hma, atr, self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle, hma_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        hma_f = float(hma_value)

        if self._prev_hma_value == 0:
            self._prev_hma_value = hma_f
            return

        slope_threshold = self._prev_hma_value * 0.0002
        is_hma_rising = hma_f - self._prev_hma_value > slope_threshold
        is_hma_falling = self._prev_hma_value - hma_f > slope_threshold

        if is_hma_rising and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif is_hma_falling and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))

        self._prev_hma_value = hma_f

    def OnReseted(self):
        super(hull_ma_trend_strategy, self).OnReseted()
        self._prev_hma_value = 0.0

    def CreateClone(self):
        return hull_ma_trend_strategy()
