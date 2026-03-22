import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class dec_ema_strategy(Strategy):

    def __init__(self):
        super(dec_ema_strategy, self).__init__()

        self._ema_period = self.Param("EmaPeriod", 3) \
            .SetDisplay("Base EMA Period", "Length for initial EMA", "Parameters")
        self._length = self.Param("Length", 15) \
            .SetDisplay("Smoothing Length", "Smoothing length for DecEMA", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._prev = 0.0
        self._prev_prev = 0.0
        self._count = 0
        self._base_ema = None
        self._ema1 = 0.0
        self._ema2 = 0.0
        self._ema3 = 0.0
        self._ema4 = 0.0
        self._ema5 = 0.0
        self._ema6 = 0.0
        self._ema7 = 0.0
        self._ema8 = 0.0
        self._ema9 = 0.0
        self._ema10 = 0.0

    @property
    def EmaPeriod(self):
        return self._ema_period.Value

    @EmaPeriod.setter
    def EmaPeriod(self, value):
        self._ema_period.Value = value

    @property
    def Length(self):
        return self._length.Value

    @Length.setter
    def Length(self, value):
        self._length.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(dec_ema_strategy, self).OnStarted(time)

        self._count = 0
        self._base_ema = ExponentialMovingAverage()
        self._base_ema.Length = self.EmaPeriod
        self._ema1 = 0.0
        self._ema2 = 0.0
        self._ema3 = 0.0
        self._ema4 = 0.0
        self._ema5 = 0.0
        self._ema6 = 0.0
        self._ema7 = 0.0
        self._ema8 = 0.0
        self._ema9 = 0.0
        self._ema10 = 0.0

        self.SubscribeCandles(self.CandleType) \
            .Bind(self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        t = candle.OpenTime

        ema_result = self._base_ema.Process(DecimalIndicatorValue(self._base_ema, close, t, True))
        if not self._base_ema.IsFormed:
            return

        ema0 = float(ema_result)
        alpha = 2.0 / (1.0 + float(self.Length))

        self._ema1 = alpha * ema0 + (1.0 - alpha) * self._ema1
        self._ema2 = alpha * self._ema1 + (1.0 - alpha) * self._ema2
        self._ema3 = alpha * self._ema2 + (1.0 - alpha) * self._ema3
        self._ema4 = alpha * self._ema3 + (1.0 - alpha) * self._ema4
        self._ema5 = alpha * self._ema4 + (1.0 - alpha) * self._ema5
        self._ema6 = alpha * self._ema5 + (1.0 - alpha) * self._ema6
        self._ema7 = alpha * self._ema6 + (1.0 - alpha) * self._ema7
        self._ema8 = alpha * self._ema7 + (1.0 - alpha) * self._ema8
        self._ema9 = alpha * self._ema8 + (1.0 - alpha) * self._ema9
        self._ema10 = alpha * self._ema9 + (1.0 - alpha) * self._ema10

        decema = (10.0 * self._ema1 - 45.0 * self._ema2 + 120.0 * self._ema3
                  - 210.0 * self._ema4 + 252.0 * self._ema5 - 210.0 * self._ema6
                  + 120.0 * self._ema7 - 45.0 * self._ema8 + 10.0 * self._ema9
                  - self._ema10)

        self._count += 1
        if self._count <= 2:
            self._prev_prev = self._prev
            self._prev = decema
            return

        if self._prev < self._prev_prev and decema > self._prev and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif self._prev > self._prev_prev and decema < self._prev and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_prev = self._prev
        self._prev = decema

    def OnReseted(self):
        super(dec_ema_strategy, self).OnReseted()
        self._prev = 0.0
        self._prev_prev = 0.0
        self._count = 0
        self._base_ema = None
        self._ema1 = 0.0
        self._ema2 = 0.0
        self._ema3 = 0.0
        self._ema4 = 0.0
        self._ema5 = 0.0
        self._ema6 = 0.0
        self._ema7 = 0.0
        self._ema8 = 0.0
        self._ema9 = 0.0
        self._ema10 = 0.0

    def CreateClone(self):
        return dec_ema_strategy()
