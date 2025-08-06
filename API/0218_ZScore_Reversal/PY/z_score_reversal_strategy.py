import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, UnitTypes, Unit, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class z_score_reversal_strategy(Strategy):
    """
    Strategy that trades based on Z-Score (normalized price deviation from the mean).
    Enters long when Z-Score is below a negative threshold (price significantly below mean).
    Enters short when Z-Score is above a positive threshold (price significantly above mean).
    Exits when Z-Score returns to zero (price returns to mean).

    """

    def __init__(self):
        super(z_score_reversal_strategy, self).__init__()

        # Constructor.
        self._lookback_period = self.Param("LookbackPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback Period", "Period for calculating mean and standard deviation", "Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 40, 5)

        self._z_score_threshold = self.Param("ZScoreThreshold", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Z-Score Threshold", "Z-Score threshold for entry signals", "Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(1.5, 3.0, 0.5)

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop-loss %", "Stop-loss as percentage of entry price", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._candle_type = self.Param("CandleType", tf(10)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._ma = None
        self._std_dev = None
        self._last_z_score = 0.0

    @property
    def lookback_period(self):
        """Period for calculating mean and standard deviation."""
        return self._lookback_period.Value

    @lookback_period.setter
    def lookback_period(self, value):
        self._lookback_period.Value = value

    @property
    def z_score_threshold(self):
        """Z-Score threshold for entry signals."""
        return self._z_score_threshold.Value

    @z_score_threshold.setter
    def z_score_threshold(self, value):
        self._z_score_threshold.Value = value

    @property
    def stop_loss_percent(self):
        """Stop-loss percentage parameter."""
        return self._stop_loss_percent.Value

    @stop_loss_percent.setter
    def stop_loss_percent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def candle_type(self):
        """Candle type parameter."""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnReseted(self):
        super(z_score_reversal_strategy, self).OnReseted()
        self._ma = None
        self._std_dev = None
        self._last_z_score = 0.0

    def OnStarted(self, time):
        super(z_score_reversal_strategy, self).OnStarted(time)

        # Initialize indicators
        self._ma = SimpleMovingAverage()
        self._ma.Length = self.lookback_period
        self._std_dev = StandardDeviation()
        self._std_dev.Length = self.lookback_period

        # Create candles subscription
        subscription = self.SubscribeCandles(self.candle_type)

        # Bind indicators to subscription
        subscription.Bind(self._ma, self._std_dev, self.ProcessCandle).Start()

        # Enable position protection with stop-loss
        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(self.stop_loss_percent, UnitTypes.Percent)
        )
        # Setup chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ma)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, ma_value, std_dev_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Skip if strategy is not ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Skip if standard deviation is zero (avoid division by zero)
        if std_dev_value == 0:
            return

        # Calculate Z-Score: (Price - Mean) / StdDev
        z_score = float((candle.ClosePrice - ma_value) / std_dev_value)

        self.LogInfo(
            "Current Z-Score: {0:.4f}, Mean: {1:.4f}, StdDev: {2:.4f}".format(
                z_score, ma_value, std_dev_value))

        # Trading logic
        if z_score < -self.z_score_threshold:
            # Long signal: Z-Score is below negative threshold
            if self.Position <= 0:
                self.BuyMarket(self.Volume + Math.Abs(self.Position))
                self.LogInfo(
                    "Long Entry: Z-Score({0:.4f}) < -{1:.4f}".format(
                        z_score, self.z_score_threshold))
        elif z_score > self.z_score_threshold:
            # Short signal: Z-Score is above positive threshold
            if self.Position >= 0:
                self.SellMarket(self.Volume + Math.Abs(self.Position))
                self.LogInfo(
                    "Short Entry: Z-Score({0:.4f}) > {1:.4f}".format(
                        z_score, self.z_score_threshold))
        elif (z_score > 0 and self.Position > 0) or (z_score < 0 and self.Position < 0):
            # Exit signals: Z-Score crossed zero line
            if self.Position > 0 and self._last_z_score < 0 and z_score > 0:
                self.SellMarket(Math.Abs(self.Position))
                self.LogInfo("Exit Long: Z-Score crossed zero from negative to positive")
            elif self.Position < 0 and self._last_z_score > 0 and z_score < 0:
                self.BuyMarket(Math.Abs(self.Position))
                self.LogInfo("Exit Short: Z-Score crossed zero from positive to negative")

        # Store current Z-Score for next calculation
        self._last_z_score = z_score

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return z_score_reversal_strategy()