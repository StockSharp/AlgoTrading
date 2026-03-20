import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergence, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class macd_cci_lotfy_strategy(Strategy):
    def __init__(self):
        super(macd_cci_lotfy_strategy, self).__init__()

        self._rsi_period = self.Param("RsiPeriod", 8)
        self._fast_period = self.Param("FastPeriod", 13)
        self._slow_period = self.Param("SlowPeriod", 33)
        self._macd_coefficient = self.Param("MacdCoefficient", 86000.0)
        self._threshold = self.Param("Threshold", 25.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30)))

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsi_period.Value = value

    @property
    def FastPeriod(self):
        return self._fast_period.Value

    @FastPeriod.setter
    def FastPeriod(self, value):
        self._fast_period.Value = value

    @property
    def SlowPeriod(self):
        return self._slow_period.Value

    @SlowPeriod.setter
    def SlowPeriod(self, value):
        self._slow_period.Value = value

    @property
    def MacdCoefficient(self):
        return self._macd_coefficient.Value

    @MacdCoefficient.setter
    def MacdCoefficient(self, value):
        self._macd_coefficient.Value = value

    @property
    def Threshold(self):
        return self._threshold.Value

    @Threshold.setter
    def Threshold(self, value):
        self._threshold.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(macd_cci_lotfy_strategy, self).OnStarted(time)

        macd = MovingAverageConvergenceDivergence()
        macd.ShortMa.Length = self.FastPeriod
        macd.LongMa.Length = self.SlowPeriod

        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(macd, rsi, self.ProcessCandle).Start()

        self.StartProtection(
            Unit(2000.0, UnitTypes.Absolute),
            Unit(1000.0, UnitTypes.Absolute))

    def ProcessCandle(self, candle, macd_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        macd_val = float(macd_value)
        rsi_val = float(rsi_value)

        coeff = float(self.MacdCoefficient)
        thresh = float(self.Threshold)
        scaled_macd = macd_val * coeff

        if rsi_val < 50.0 - thresh and scaled_macd < -thresh:
            if self.Position <= 0:
                self.BuyMarket()
        elif rsi_val > 50.0 + thresh and scaled_macd > thresh:
            if self.Position >= 0:
                self.SellMarket()

    def OnReseted(self):
        super(macd_cci_lotfy_strategy, self).OnReseted()

    def CreateClone(self):
        return macd_cci_lotfy_strategy()
