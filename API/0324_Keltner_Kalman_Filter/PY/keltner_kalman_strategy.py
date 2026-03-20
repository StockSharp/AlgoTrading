import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class keltner_kalman_strategy(Strategy):
    """
    Strategy combining Keltner Channels with a Kalman Filter to identify trends and trade opportunities.
    """

    def __init__(self):
        super(keltner_kalman_strategy, self).__init__()

        self._ema_period = self.Param("EmaPeriod", 20) \
            .SetDisplay("EMA Period", "EMA period for Keltner Channel", "Keltner Channel") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 30, 5)

        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "ATR period for Keltner Channel", "Keltner Channel") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 20, 2)

        self._atr_multiplier = self.Param("AtrMultiplier", 2.0) \
            .SetDisplay("ATR Multiplier", "ATR multiplier for Keltner Channel", "Keltner Channel") \
            .SetCanOptimize(True) \
            .SetOptimize(1.5, 3.0, 0.5)

        self._kalman_process_noise = self.Param("KalmanProcessNoise", 0.01) \
            .SetDisplay("Kalman Process Noise (Q)", "Kalman filter process noise parameter", "Kalman Filter") \
            .SetCanOptimize(True) \
            .SetOptimize(0.001, 0.1, 0.005)

        self._kalman_measurement_noise = self.Param("KalmanMeasurementNoise", 0.1) \
            .SetDisplay("Kalman Measurement Noise (R)", "Kalman filter measurement noise parameter", "Kalman Filter") \
            .SetCanOptimize(True) \
            .SetOptimize(0.01, 1.0, 0.05)

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._ema = None
        self._atr = None
        self._kalman_estimate = 0.0
        self._kalman_error = 1.0
        self._prices = []
        self._ema_value = 0.0
        self._atr_value = 0.0
        self._upper_band = 0.0
        self._lower_band = 0.0

    @property
    def EmaPeriod(self):
        return self._ema_period.Value

    @EmaPeriod.setter
    def EmaPeriod(self, value):
        self._ema_period.Value = value

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
    def KalmanProcessNoise(self):
        return self._kalman_process_noise.Value

    @KalmanProcessNoise.setter
    def KalmanProcessNoise(self, value):
        self._kalman_process_noise.Value = value

    @property
    def KalmanMeasurementNoise(self):
        return self._kalman_measurement_noise.Value

    @KalmanMeasurementNoise.setter
    def KalmanMeasurementNoise(self, value):
        self._kalman_measurement_noise.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(keltner_kalman_strategy, self).OnReseted()
        self._kalman_estimate = 0.0
        self._kalman_error = 1.0
        self._prices = []
        self._ema_value = 0.0
        self._atr_value = 0.0
        self._upper_band = 0.0
        self._lower_band = 0.0
        self._ema = None
        self._atr = None

    def OnStarted(self, time):
        super(keltner_kalman_strategy, self).OnStarted(time)

        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.EmaPeriod
        self._atr = AverageTrueRange()
        self._atr.Length = self.AtrPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._ema, self._atr, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ema)
            self.DrawOwnTrades(area)

        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(2, UnitTypes.Percent)
        )

    def ProcessCandle(self, candle, ema_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        self._ema_value = float(ema_value)
        self._atr_value = float(atr_value)

        self._upper_band = self._ema_value + (self._atr_value * self.AtrMultiplier)
        self._lower_band = self._ema_value - (self._atr_value * self.AtrMultiplier)

        price = float(candle.ClosePrice)
        self.UpdateKalmanFilter(price)

        self._prices.append(price)
        if len(self._prices) > 10:
            self._prices.pop(0)

        kalman_slope = self.CalculateKalmanSlope()

        if self.Position != 0:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if price > self._upper_band and kalman_slope > 0:
            self.BuyMarket()
        elif price < self._lower_band and kalman_slope < 0:
            self.SellMarket()

    def UpdateKalmanFilter(self, price):
        predicted_estimate = self._kalman_estimate
        predicted_error = self._kalman_error + self.KalmanProcessNoise

        kalman_gain = predicted_error / (predicted_error + self.KalmanMeasurementNoise)
        self._kalman_estimate = predicted_estimate + kalman_gain * (price - predicted_estimate)
        self._kalman_error = (1.0 - kalman_gain) * predicted_error

    def CalculateKalmanSlope(self):
        if len(self._prices) < 3:
            return 0.0

        n = len(self._prices)
        sum_x = 0.0
        sum_y = 0.0
        sum_xy = 0.0
        sum_x2 = 0.0

        for i in range(n):
            x = float(i)
            y = self._prices[i]
            sum_x += x
            sum_y += y
            sum_xy += x * y
            sum_x2 += x * x

        denominator = n * sum_x2 - sum_x * sum_x
        if denominator == 0:
            return 0.0

        slope = (n * sum_xy - sum_x * sum_y) / denominator
        return slope

    def CreateClone(self):
        return keltner_kalman_strategy()
