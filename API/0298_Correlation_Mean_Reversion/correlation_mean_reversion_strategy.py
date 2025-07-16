import clr

clr.AddReference("System")
clr.AddReference("System.Collections")
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Math
from System.Collections.Generic import Queue
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, ICandleMessage
from StockSharp.Algo.Indicators import SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *
from StockSharp.BusinessEntities import Security


class correlation_mean_reversion_strategy(Strategy):
    """
    Correlation Mean Reversion strategy.
    Trades based on changes in correlation between two securities.
    """

    def __init__(self):
        super(correlation_mean_reversion_strategy, self).__init__()

        # Indicators and data containers
        self._correlation_sma = None
        self._correlation_std_dev = None

        self._security1_prices = Queue[float]()
        self._security2_prices = Queue[float]()

        # Current values
        self._current_correlation = 0.0
        self._average_correlation = 0.0
        self._correlation_std_deviation = 0.0
        self._security1_last_price = 0.0
        self._security2_last_price = 0.0
        self._security1_updated = False
        self._security2_updated = False

        # Initialize strategy parameters
        self._security2 = self.Param("Security2", None) \
            .SetDisplay("Second Security", "Second security for correlation calculation", "Securities")

        self._correlation_period = self.Param("CorrelationPeriod", 20) \
            .SetRange(10, 100) \
            .SetDisplay("Correlation Period", "Period for correlation calculation", "Parameters") \
            .SetCanOptimize(True)

        self._lookback_period = self.Param("LookbackPeriod", 20) \
            .SetRange(10, 100) \
            .SetDisplay("Lookback Period", "Period for moving average and standard deviation calculation", "Parameters") \
            .SetCanOptimize(True)

        self._deviation_threshold = self.Param("DeviationThreshold", 2.0) \
            .SetRange(1.0, 3.0) \
            .SetDisplay("Deviation Threshold", "Threshold in standard deviations for entry signals", "Parameters") \
            .SetCanOptimize(True)

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetRange(0.5, 5.0) \
            .SetDisplay("Stop Loss", "Stop loss percentage from entry price", "Parameters") \
            .SetCanOptimize(True)

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Timeframe for candles", "Parameters")

    # First security for correlation calculation.
    @property
    def Security1(self):
        return self.Security

    @Security1.setter
    def Security1(self, value):
        self.Security = value

    # Second security for correlation calculation.
    @property
    def Security2(self):
        return self._security2.Value

    @Security2.setter
    def Security2(self, value):
        self._security2.Value = value

    # Period for correlation calculation.
    @property
    def CorrelationPeriod(self):
        return self._correlation_period.Value

    @CorrelationPeriod.setter
    def CorrelationPeriod(self, value):
        self._correlation_period.Value = value

    # Lookback period for moving average and standard deviation calculation.
    @property
    def LookbackPeriod(self):
        return self._lookback_period.Value

    @LookbackPeriod.setter
    def LookbackPeriod(self, value):
        self._lookback_period.Value = value

    # Threshold in standard deviations for entry signals.
    @property
    def DeviationThreshold(self):
        return self._deviation_threshold.Value

    @DeviationThreshold.setter
    def DeviationThreshold(self, value):
        self._deviation_threshold.Value = value

    # Stop loss percentage from entry price.
    @property
    def StopLossPercent(self):
        return self._stop_loss_percent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stop_loss_percent.Value = value

    # Candle timeframe type for data subscription.
    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        if self.Security1 is not None and self.Security2 is not None:
            return [
                (self.Security1, self.CandleType),
                (self.Security2, self.CandleType),
            ]
        return []

    def OnStarted(self, time):
        super(correlation_mean_reversion_strategy, self).OnStarted(time)

        self._current_correlation = 0
        self._average_correlation = 0
        self._correlation_std_deviation = 0
        self._security1_last_price = 0
        self._security2_last_price = 0
        self._security1_updated = False
        self._security2_updated = False
        self._security1_prices.Clear()
        self._security2_prices.Clear()

        if self.Security1 is None:
            raise Exception("First security is not specified.")
        if self.Security2 is None:
            raise Exception("Second security is not specified.")

        # Initialize indicators
        self._correlation_sma = SimpleMovingAverage()
        self._correlation_sma.Length = self.LookbackPeriod
        self._correlation_std_dev = StandardDeviation()
        self._correlation_std_dev.Length = self.LookbackPeriod

        # Subscribe to candles for both securities
        subscription1 = self.SubscribeCandles(self.CandleType, False, self.Security1)
        subscription2 = self.SubscribeCandles(self.CandleType, False, self.Security2)

        # Process data
        subscription1.Bind(self.ProcessSecurity1Candle).Start()
        subscription2.Bind(self.ProcessSecurity2Candle).Start()

        # Setup visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription1)
            self.DrawCandles(area, subscription2)
            self.DrawOwnTrades(area)

        # Setup position protection
        self.StartProtection(
            Unit(0, UnitTypes.Absolute),  # No take profit
            Unit(self.StopLossPercent, UnitTypes.Percent),  # Stop loss in percent
            False  # No trailing stop
        )

    def ProcessSecurity1Candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        # Store the last price
        self._security1_last_price = candle.ClosePrice
        self._security1_updated = True

        # Update correlation and check for signals
        self.CalculateCorrelation(candle.ServerTime, candle.State == CandleStates.Finished)

    def ProcessSecurity2Candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        # Store the last price
        self._security2_last_price = candle.ClosePrice
        self._security2_updated = True

        # Update correlation and check for signals
        self.CalculateCorrelation(candle.ServerTime, candle.State == CandleStates.Finished)

    def CalculateCorrelation(self, time, is_final):
        # Only proceed if both securities have been updated
        if not self._security1_updated or not self._security2_updated:
            return

        # Reset flags
        self._security1_updated = False
        self._security2_updated = False

        # Add the latest prices to queues
        self._security1_prices.Enqueue(self._security1_last_price)
        self._security2_prices.Enqueue(self._security2_last_price)

        # Keep queue sizes limited to correlation period
        while self._security1_prices.Count > self.CorrelationPeriod:
            self._security1_prices.Dequeue()
            self._security2_prices.Dequeue()

        # Need sufficient data to calculate correlation
        if self._security1_prices.Count < self.CorrelationPeriod:
            return

        # Calculate correlation coefficient
        series1 = list(self._security1_prices)
        series2 = list(self._security2_prices)
        self._current_correlation = self.CalculateCorrelationCoefficient(series1, series2)

        # Process indicators
        self._average_correlation = to_float(self._correlation_sma.Process(self._current_correlation, time, is_final))
        self._correlation_std_deviation = to_float(self._correlation_std_dev.Process(self._current_correlation, time, is_final))

        if self._correlation_std_deviation == 0:
            return

        # Check for trading signals
        self.CheckSignal()

    @staticmethod
    def CalculateCorrelationCoefficient(series1, series2):
        # Need at least two points for correlation
        if len(series1) < 2 or len(series1) != len(series2):
            return 0

        # Calculate means
        mean1 = sum(series1) / len(series1)
        mean2 = sum(series2) / len(series2)

        sum1 = 0
        sum2 = 0
        sum12 = 0

        # Calculate correlation
        for i in range(len(series1)):
            diff1 = series1[i] - mean1
            diff2 = series2[i] - mean2

            sum1 += diff1 * diff1
            sum2 += diff2 * diff2
            sum12 += diff1 * diff2

        # Avoid division by zero
        if sum1 == 0 or sum2 == 0:
            return 0

        denom = Math.Sqrt(float(sum1 * sum2))
        return 0 if denom == 0 else sum12 / denom

    def CheckSignal(self):
        # Ensure strategy is ready for trading and indicators are formed
        if (not self.IsFormedAndOnlineAndAllowTrading() or
                not self._correlation_sma.IsFormed or
                not self._correlation_std_dev.IsFormed):
            return

        # Calculate Z-score for correlation
        correlation_z_score = (self._current_correlation - self._average_correlation) / self._correlation_std_deviation

        # If we have no position, check for entry signals
        if self.Position == 0:
            # Correlation drops below average (securities are less correlated than normal)
            if correlation_z_score < -self.DeviationThreshold:
                # Long Security1, Short Security2
                self.BuyMarket(self.Volume, self.Security1)
                self.SellMarket(self.Volume, self.Security2)

                self.LogInfo(f"LONG {self.Security1.Code}, SHORT {self.Security2.Code}: Correlation Z-Score: {correlation_z_score:F2}")
            # Correlation rises above average (securities are more correlated than normal)
            elif correlation_z_score > self.DeviationThreshold:
                # Short Security1, Long Security2
                self.SellMarket(self.Volume, self.Security1)
                self.BuyMarket(self.Volume, self.Security2)

                self.LogInfo(f"SHORT {self.Security1.Code}, LONG {self.Security2.Code}: Correlation Z-Score: {correlation_z_score:F2}")
        # Check for exit signals
        else:
            # Exit when correlation returns to average
            if ((self.Position > 0 and correlation_z_score >= 0) or
                    (self.Position < 0 and correlation_z_score <= 0)):
                self.ClosePosition(self.Security1)
                self.ClosePosition(self.Security2)

                self.LogInfo(f"CLOSE PAIR: Correlation Z-Score: {correlation_z_score:F2}")

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return correlation_mean_reversion_strategy()
