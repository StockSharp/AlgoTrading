import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, UnitTypes, Unit, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class z_score_strategy(Strategy):
    """
    Strategy based on Z-Score indicator for mean reversion trading.
    Z-Score measures the distance from the price to its moving average in standard deviations.
    
    """
    
    def __init__(self):
        super(z_score_strategy, self).__init__()
        
        # Initialize strategy parameters
        self._z_score_entry_threshold = self.Param("ZScoreEntryThreshold", 2.0) \
            .SetDisplay("Z-Score Entry Threshold", "Distance from mean in std deviations required to enter position", "Z-Score Parameters")
        
        self._z_score_exit_threshold = self.Param("ZScoreExitThreshold", 0.0) \
            .SetDisplay("Z-Score Exit Threshold", "Distance from mean in std deviations required to exit position", "Z-Score Parameters")
        
        self._ma_period = self.Param("MAPeriod", 20) \
            .SetDisplay("MA Period", "Period for Moving Average calculation", "Technical Parameters")
        
        self._std_dev_period = self.Param("StdDevPeriod", 20) \
            .SetDisplay("StdDev Period", "Period for Standard Deviation calculation", "Technical Parameters")
        
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss as percentage from entry price", "Risk Management")
        
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "Data")
        
        # State tracking
        self._prev_z_score = 0.0

    @property
    def z_score_entry_threshold(self):
        """Z-Score threshold for entry (default: 2.0)"""
        return self._z_score_entry_threshold.Value

    @z_score_entry_threshold.setter
    def z_score_entry_threshold(self, value):
        self._z_score_entry_threshold.Value = value

    @property
    def z_score_exit_threshold(self):
        """Z-Score threshold for exit (default: 0.0)"""
        return self._z_score_exit_threshold.Value

    @z_score_exit_threshold.setter
    def z_score_exit_threshold(self, value):
        self._z_score_exit_threshold.Value = value

    @property
    def ma_period(self):
        """Period for Moving Average calculation (default: 20)"""
        return self._ma_period.Value

    @ma_period.setter
    def ma_period(self, value):
        self._ma_period.Value = value

    @property
    def std_dev_period(self):
        """Period for Standard Deviation calculation (default: 20)"""
        return self._std_dev_period.Value

    @std_dev_period.setter
    def std_dev_period(self, value):
        self._std_dev_period.Value = value

    @property
    def stop_loss_percent(self):
        """Stop-loss as percentage from entry price (default: 2%)"""
        return self._stop_loss_percent.Value

    @stop_loss_percent.setter
    def stop_loss_percent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def candle_type(self):
        """Type of candles used for strategy calculation"""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        """
        Resets internal state when strategy is reset.
        """
        super(z_score_strategy, self).OnReseted()
        self._prev_z_score = 0.0

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(z_score_strategy, self).OnStarted(time)

        # Reset state variables
        self._prev_z_score = 0.0

        # Create indicators
        sma = SimpleMovingAverage()
        sma.Length = self.ma_period
        
        std_dev = StandardDeviation()
        std_dev.Length = self.std_dev_period

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, std_dev, self.ProcessCandle).Start()

        # Configure chart
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

        # Setup protection with stop-loss
        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(self.stop_loss_percent, UnitTypes.Percent)
        )
    def ProcessCandle(self, candle, ma_value, std_dev_value):
        """
        Process candle and calculate Z-Score
        
        :param candle: The processed candle message.
        :param ma_value: The current value of the moving average.
        :param std_dev_value: The current value of the standard deviation.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Convert values to float
        ma_decimal = float(ma_value)
        std_dev_decimal = float(std_dev_value)

        # Calculate Z-Score: (price - MA) / StdDev
        # Avoid division by zero
        if std_dev_decimal == 0:
            return

        z_score = float((candle.ClosePrice - ma_decimal) / std_dev_decimal)

        # Process trading signals
        if self.Position == 0:
            # No position - check for entry signals
            if z_score < -self.z_score_entry_threshold:
                # Price is below MA by more than threshold std deviations - buy (long)
                self.BuyMarket(self.Volume)
                self.LogInfo("Buy signal: Z-Score {0:F2} below entry threshold -{1:F2}".format(
                    z_score, self.z_score_entry_threshold))
            elif z_score > self.z_score_entry_threshold:
                # Price is above MA by more than threshold std deviations - sell (short)
                self.SellMarket(self.Volume)
                self.LogInfo("Sell signal: Z-Score {0:F2} above entry threshold {1:F2}".format(
                    z_score, self.z_score_entry_threshold))
        elif self.Position > 0:
            # Long position - check for exit signal
            if z_score > self.z_score_exit_threshold:
                # Price has returned to or above mean - exit long
                self.SellMarket(self.Position)
                self.LogInfo("Exit long: Z-Score {0:F2} above exit threshold {1:F2}".format(
                    z_score, self.z_score_exit_threshold))
        elif self.Position < 0:
            # Short position - check for exit signal
            if z_score < self.z_score_exit_threshold:
                # Price has returned to or below mean - exit short
                self.BuyMarket(Math.Abs(self.Position))
                self.LogInfo("Exit short: Z-Score {0:F2} below exit threshold {1:F2}".format(
                    z_score, self.z_score_exit_threshold))

        # Store current Z-Score for later use
        self._prev_z_score = z_score

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return z_score_strategy()