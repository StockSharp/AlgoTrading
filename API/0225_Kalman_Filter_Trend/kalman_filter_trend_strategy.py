import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import KalmanFilter, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class kalman_filter_trend_strategy(Strategy):
    """
    Kalman Filter Trend strategy.
    Uses a custom Kalman Filter indicator to track price trend.
    """

    def __init__(self):
        super(kalman_filter_trend_strategy, self).__init__()

        # Process noise coefficient for Kalman filter.
        self._process_noise = self.Param("ProcessNoise", 0.01) \
            .SetRange(0.0001, 1) \
            .SetDisplay("Process Noise", "Process noise coefficient for Kalman filter", "Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(0.001, 0.1, 0.005)

        # Measurement noise coefficient for Kalman filter.
        self._measurement_noise = self.Param("MeasurementNoise", 0.1) \
            .SetRange(0.0001, 1) \
            .SetDisplay("Measurement Noise", "Measurement noise coefficient for Kalman filter", "Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(0.01, 1.0, 0.1)

        # Candle type for strategy.
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Candle type for strategy", "Common")

    @property
    def ProcessNoise(self):
        return self._process_noise.Value

    @ProcessNoise.setter
    def ProcessNoise(self, value):
        self._process_noise.Value = value

    @property
    def MeasurementNoise(self):
        return self._measurement_noise.Value

    @MeasurementNoise.setter
    def MeasurementNoise(self, value):
        self._measurement_noise.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        super(kalman_filter_trend_strategy, self).OnStarted(time)

        # Create indicators
        self._kalman_filter = KalmanFilter()
        self._kalman_filter.ProcessNoise = self.ProcessNoise
        self._kalman_filter.MeasurementNoise = self.MeasurementNoise

        self._atr = AverageTrueRange()
        self._atr.Length = 14

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(self._kalman_filter, self._atr, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._kalman_filter)
            self.DrawOwnTrades(area)

        # Enable position protection
        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(2, UnitTypes.Absolute)
        )
    def ProcessCandle(self, candle, kalman_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Calculate trend direction
        trend = 1 if candle.ClosePrice > float(kalman_value) else -1

        # Trading logic based on price position relative to Kalman filter
        if trend > 0 and self.Position <= 0:
            # Buy when price is above Kalman filter (uptrend)
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif trend < 0 and self.Position >= 0:
            # Sell when price is below Kalman filter (downtrend)
            self.SellMarket(self.Volume + Math.Abs(self.Position))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return kalman_filter_trend_strategy()
