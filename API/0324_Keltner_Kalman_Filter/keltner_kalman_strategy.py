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

        # Kalman filter parameters
        self._kalman_estimate = 0
        self._kalman_error = 1
        self._prices = []

        # Saved values for decision making
        self._ema_value = 0
        self._atr_value = 0
        self._upper_band = 0
        self._lower_band = 0
        self._is_long_position = False
        self._is_short_position = False

        # EMA period for Keltner Channel.
        self._ema_period = self.Param("EmaPeriod", 20) \
            .SetDisplay("EMA Period", "EMA period for Keltner Channel", "Keltner Channel") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 30, 5)

        # ATR period for Keltner Channel.
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "ATR period for Keltner Channel", "Keltner Channel") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 20, 2)

        # ATR multiplier for Keltner Channel.
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0) \
            .SetDisplay("ATR Multiplier", "ATR multiplier for Keltner Channel", "Keltner Channel") \
            .SetCanOptimize(True) \
            .SetOptimize(1.5, 3.0, 0.5)

        # Kalman filter process noise parameter (Q).
        self._kalman_process_noise = self.Param("KalmanProcessNoise", 0.01) \
            .SetDisplay("Kalman Process Noise (Q)", "Kalman filter process noise parameter", "Kalman Filter") \
            .SetCanOptimize(True) \
            .SetOptimize(0.001, 0.1, 0.005)

        # Kalman filter measurement noise parameter (R).
        self._kalman_measurement_noise = self.Param("KalmanMeasurementNoise", 0.1) \
            .SetDisplay("Kalman Measurement Noise (R)", "Kalman filter measurement noise parameter", "Kalman Filter") \
            .SetCanOptimize(True) \
            .SetOptimize(0.01, 1.0, 0.05)

        # Candle type to use for the strategy.
        self._candle_type = self.Param("CandleType", tf(15)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._ema = None
        self._atr = None

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

    def OnStarted(self, time):
        super(keltner_kalman_strategy, self).OnStarted(time)

        # Initialize Kalman filter
        self._kalman_estimate = 0
        self._kalman_error = 1
        self._prices.clear()
        self._is_long_position = False
        self._is_short_position = False
        self._ema_value = 0
        self._atr_value = 0
        self._upper_band = 0
        self._lower_band = 0

        # Create indicators
        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.EmaPeriod

        self._atr = AverageTrueRange()
        self._atr.Length = self.AtrPeriod

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(self._ema, self._atr, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ema)
            self.DrawOwnTrades(area)

        # Setup position protection
        self.StartProtection(
            Unit(2, UnitTypes.Percent),
            Unit(2, UnitTypes.Percent)
        )

    def ProcessCandle(self, candle, ema_value, atr_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Save indicator values
        self._ema_value = ema_value
        self._atr_value = atr_value

        # Calculate Keltner Channels
        self._upper_band = self._ema_value + (self._atr_value * self.AtrMultiplier)
        self._lower_band = self._ema_value - (self._atr_value * self.AtrMultiplier)

        # Update Kalman filter
        self.UpdateKalmanFilter(candle.ClosePrice)

        # Store prices for slope calculation
        self._prices.append(candle.ClosePrice)
        if len(self._prices) > 10:
            self._prices.pop(0)

        # Calculate Kalman slope (trend direction)
        kalman_slope = self.CalculateKalmanSlope()

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Trading logic
        # Buy when price is above EMA+k*ATR (upper band) and Kalman filter shows uptrend
        if candle.ClosePrice > self._upper_band and self._kalman_estimate > candle.ClosePrice and kalman_slope > 0 and self.Position <= 0:
            self.BuyMarket(self.Volume)
            self.LogInfo("Buy Signal: Price {0:F2} > Upper Band {1:F2}, Kalman Estimate {2:F2}, Kalman Slope {3:F6}".format(
                candle.ClosePrice, self._upper_band, self._kalman_estimate, kalman_slope))
            self._is_long_position = True
            self._is_short_position = False
        # Sell when price is below EMA-k*ATR (lower band) and Kalman filter shows downtrend
        elif candle.ClosePrice < self._lower_band and self._kalman_estimate < candle.ClosePrice and kalman_slope < 0 and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))
            self.LogInfo("Sell Signal: Price {0:F2} < Lower Band {1:F2}, Kalman Estimate {2:F2}, Kalman Slope {3:F6}".format(
                candle.ClosePrice, self._lower_band, self._kalman_estimate, kalman_slope))
            self._is_long_position = False
            self._is_short_position = True
        # Exit long position when price falls below EMA
        elif self._is_long_position and candle.ClosePrice < self._ema_value:
            self.SellMarket(self.Position)
            self.LogInfo("Exit Long: Price {0:F2} fell below EMA {1:F2}".format(
                candle.ClosePrice, self._ema_value))
            self._is_long_position = False
        # Exit short position when price rises above EMA
        elif self._is_short_position and candle.ClosePrice > self._ema_value:
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo("Exit Short: Price {0:F2} rose above EMA {1:F2}".format(
                candle.ClosePrice, self._ema_value))
            self._is_short_position = False

    def UpdateKalmanFilter(self, price):
        # Kalman filter implementation (one-dimensional)
        # Prediction step
        predicted_estimate = self._kalman_estimate
        predicted_error = self._kalman_error + self.KalmanProcessNoise

        # Update step
        kalman_gain = predicted_error / (predicted_error + self.KalmanMeasurementNoise)
        self._kalman_estimate = predicted_estimate + kalman_gain * (price - predicted_estimate)
        self._kalman_error = (1 - kalman_gain) * predicted_error

        self.LogInfo(
            "Kalman Filter: Price {0:F2}, Estimate {1:F2}, Error {2:F6}, Gain {3:F6}".format(
                price, self._kalman_estimate, self._kalman_error, kalman_gain))

    def CalculateKalmanSlope(self):
        # Need at least a few points to calculate a slope
        if len(self._prices) < 3:
            return 0

        # Simple linear regression slope calculation
        n = len(self._prices)
        sumX = 0
        sumY = 0
        sumXY = 0
        sumX2 = 0

        for i, y in enumerate(self._prices):
            x = i
            sumX += x
            sumY += y
            sumXY += x * y
            sumX2 += x * x

        denominator = n * sumX2 - sumX * sumX

        if denominator == 0:
            return 0

        slope = (n * sumXY - sumX * sumY) / denominator
        return slope

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return keltner_kalman_strategy()
