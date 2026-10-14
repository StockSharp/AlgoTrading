import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import HullMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class hull_ma_trend_strategy(Strategy):
    """
    Strategy based on Hull Moving Average trend.
    It turns long when the Hull MA starts rising and short when it starts falling,
    and protects the position with a stop that trails the close by AtrMultiplier ATRs.
    """

    def __init__(self):
        super(hull_ma_trend_strategy, self).__init__()

        self._hma_period = self.Param("HmaPeriod", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("HMA Period", "Period for Hull Moving Average", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "Period for Average True Range (stop-loss)", "Risk parameters")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Multiplier", "Distance of the trailing stop in ATR multiples", "Risk parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_hma_value = None
        # Direction of the last slope change: 1 rising, -1 falling, 0 none yet.
        self._slope = 0
        self._stop_price = Decimal(0)

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

    def OnStarted2(self, time):
        super(hull_ma_trend_strategy, self).OnStarted2(time)

        hma = HullMovingAverage()
        hma.Length = self.HmaPeriod
        atr = AverageTrueRange()
        atr.Length = self.AtrPeriod

        self.SubscribeCandles(self.CandleType) \
            .BindEx(hma, atr, self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle, hma_indicator_value, atr_indicator_value):
        if candle.State != CandleStates.Finished or not hma_indicator_value.IsFormed or not atr_indicator_value.IsFormed:
            return

        hma_value = hma_indicator_value.GetValue[Decimal](None)
        atr_value = atr_indicator_value.GetValue[Decimal](None)

        previous = self._prev_hma_value
        self._prev_hma_value = hma_value

        if previous is None or not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = candle.ClosePrice
        distance = Decimal(self.AtrMultiplier) * atr_value
        slope = 1 if hma_value > previous else (-1 if hma_value < previous else 0)

        # Only a change of the slope direction is a new signal, so a stopped trade is not reopened at once.
        if slope != 0 and slope != self._slope:
            self._slope = slope

            if slope > 0 and self.Position <= 0:
                self.BuyMarket(self.Volume + Math.Abs(self.Position))
                self._stop_price = close - distance
                return

            if slope < 0 and self.Position >= 0:
                self.SellMarket(self.Volume + Math.Abs(self.Position))
                self._stop_price = close + distance
                return

        if self.Position > 0:
            if close <= self._stop_price:
                self.SellMarket(self.Position)
            else:
                self._stop_price = Math.Max(self._stop_price, close - distance)
        elif self.Position < 0:
            if close >= self._stop_price:
                self.BuyMarket(-self.Position)
            else:
                self._stop_price = Math.Min(self._stop_price, close + distance)

    def OnReseted(self):
        super(hull_ma_trend_strategy, self).OnReseted()
        self._prev_hma_value = None
        self._slope = 0
        self._stop_price = Decimal(0)

    def CreateClone(self):
        return hull_ma_trend_strategy()
