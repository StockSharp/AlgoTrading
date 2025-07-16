import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, KalmanFilter
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *


class bollinger_kalman_filter_strategy(Strategy):
    """
    Bollinger Bands with Kalman Filter Strategy.
    Enters positions when price is at Bollinger extremes and confirmed by Kalman Filter trend direction.
    """

    def __init__(self):
        super(bollinger_kalman_filter_strategy, self).__init__()

        # Initialize strategy.
        self._bollinger_length = self.Param("BollingerLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Bollinger Length", "Length of the Bollinger Bands", "Bollinger Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 30, 5)

        self._bollinger_deviation = self.Param("BollingerDeviation", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Bollinger Deviation", "Standard deviation multiplier for Bollinger Bands", "Bollinger Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(1.5, 2.5, 0.5)

        self._kalman_q = self.Param("KalmanQ", 0.01) \
            .SetGreaterThanZero() \
            .SetDisplay("Kalman Q", "Process noise for Kalman Filter", "Kalman Filter Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(0.001, 0.1, 0.01)

        self._kalman_r = self.Param("KalmanR", 0.1) \
            .SetGreaterThanZero() \
            .SetDisplay("Kalman R", "Measurement noise for Kalman Filter", "Kalman Filter Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(0.01, 1.0, 0.1)

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def BollingerLength(self):
        """Bollinger Bands length."""
        return self._bollinger_length.Value

    @BollingerLength.setter
    def BollingerLength(self, value):
        self._bollinger_length.Value = value

    @property
    def BollingerDeviation(self):
        """Bollinger Bands deviation."""
        return self._bollinger_deviation.Value

    @BollingerDeviation.setter
    def BollingerDeviation(self, value):
        self._bollinger_deviation.Value = value

    @property
    def KalmanQ(self):
        """Kalman Filter process noise."""
        return self._kalman_q.Value

    @KalmanQ.setter
    def KalmanQ(self, value):
        self._kalman_q.Value = value

    @property
    def KalmanR(self):
        """Kalman Filter measurement noise."""
        return self._kalman_r.Value

    @KalmanR.setter
    def KalmanR(self, value):
        self._kalman_r.Value = value

    @property
    def CandleType(self):
        """Candle type for strategy calculation."""
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        super(bollinger_kalman_filter_strategy, self).OnStarted(time)

        # Create indicators
        bollinger = BollingerBands()
        bollinger.Length = self.BollingerLength
        bollinger.Width = self.BollingerDeviation

        kalman_filter = KalmanFilter()
        kalman_filter.ProcessNoise = self.KalmanQ
        kalman_filter.MeasurementNoise = self.KalmanR

        # Create subscription for candles
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind indicators to the subscription
        subscription.BindEx(bollinger, kalman_filter, self.ProcessCandle).Start()

        # Start position protection
        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(1, UnitTypes.Percent)
        )
        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)
            self.DrawIndicator(area, kalman_filter)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, bollinger_value, kalman_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Extract values from indicators
        upper_band = bollinger_value.UpBand
        if upper_band is None:
            return
        lower_band = bollinger_value.LowBand
        if lower_band is None:
            return
        mid_band = bollinger_value.MovingAverage
        if mid_band is None:
            return

        kalman_filter_value = to_float(kalman_value)

        # Log the values
        self.LogInfo(
            "Price: {0}, Kalman: {1}, BB middle: {2}, BB upper: {3}, BB lower: {4}".format(
                candle.ClosePrice, kalman_filter_value, mid_band, upper_band, lower_band
            )
        )

        # Trading logic: Buy when price is below lower band but Kalman filter shows upward trend
        if candle.ClosePrice < lower_band and kalman_filter_value > candle.ClosePrice and self.Position <= 0:
            # If we have a short position, close it first
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))

            # Open a long position
            self.BuyMarket(self.Volume)
            self.LogInfo(
                "Buy signal: Price below lower band ({0} < {1}) with Kalman uptrend ({2} > {0})".format(
                    candle.ClosePrice, lower_band, kalman_filter_value
                )
            )
        # Trading logic: Sell when price is above upper band but Kalman filter shows downward trend
        elif candle.ClosePrice > upper_band and kalman_filter_value < candle.ClosePrice and self.Position >= 0:
            # If we have a long position, close it first
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))

            # Open a short position
            self.SellMarket(self.Volume)
            self.LogInfo(
                "Sell signal: Price above upper band ({0} > {1}) with Kalman downtrend ({2} < {0})".format(
                    candle.ClosePrice, upper_band, kalman_filter_value
                )
            )
        # Exit signals
        elif (self.Position > 0 and candle.ClosePrice > mid_band) or (
            self.Position < 0 and candle.ClosePrice < mid_band
        ):
            # Close position when price returns to middle band
            self.ClosePosition()
            self.LogInfo(
                "Exit signal: Price returned to middle band. Position closed at {0}".format(
                    candle.ClosePrice
                )
            )

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return bollinger_kalman_filter_strategy()
