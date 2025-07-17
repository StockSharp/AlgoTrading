import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Security
from datatype_extensions import *
from indicator_extensions import *

class correlation_breakout_strategy(Strategy):
    """
    Strategy that trades based on correlation breakout between two assets.
    """

    def __init__(self):
        super(correlation_breakout_strategy, self).__init__()

        # First asset for correlation
        self._asset1_param = self.Param("Asset1", None) \
            .SetDisplay("Asset 1", "First asset for correlation", "Instruments")

        # Second asset for correlation
        self._asset2_param = self.Param("Asset2", None) \
            .SetDisplay("Asset 2", "Second asset for correlation", "Instruments")

        # Candle type for data
        self._candle_type_param = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Period for calculating correlations
        self._lookback_period_param = self.Param("LookbackPeriod", 20) \
            .SetDisplay("Lookback Period", "Period for calculating correlations", "Strategy") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5) \
            .SetGreaterThanZero()

        # Threshold multiplier for standard deviation
        self._threshold_param = self.Param("Threshold", 2.0) \
            .SetDisplay("Threshold", "Threshold multiplier for standard deviation", "Strategy") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5) \
            .SetNotNegative()

        # Stop loss percentage
        self._stop_loss_percent_param = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management") \
            .SetNotNegative()

        self._asset1_prices = None
        self._asset2_prices = None
        self._current_index = 0
        self._corr_std_dev = StandardDeviation()
        self._avg_correlation = 0.0
        self._last_correlation = 0.0
        self._is_initialized = False

    @property
    def Asset1(self):
        """First asset for correlation."""
        return self._asset1_param.Value

    @Asset1.setter
    def Asset1(self, value):
        self._asset1_param.Value = value

    @property
    def Asset2(self):
        """Second asset for correlation."""
        return self._asset2_param.Value

    @Asset2.setter
    def Asset2(self, value):
        self._asset2_param.Value = value

    @property
    def CandleType(self):
        """Candle type for data."""
        return self._candle_type_param.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type_param.Value = value

    @property
    def LookbackPeriod(self):
        """Period for calculating correlations."""
        return self._lookback_period_param.Value

    @LookbackPeriod.setter
    def LookbackPeriod(self, value):
        self._lookback_period_param.Value = value
        if self._asset1_prices is not None:
            self._asset1_prices = [0.0] * value
            self._asset2_prices = [0.0] * value

    @property
    def Threshold(self):
        """Threshold multiplier for standard deviation."""
        return self._threshold_param.Value

    @Threshold.setter
    def Threshold(self, value):
        self._threshold_param.Value = value

    @property
    def StopLossPercent(self):
        """Stop loss percentage."""
        return self._stop_loss_percent_param.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stop_loss_percent_param.Value = value

    def GetWorkingSecurities(self):
        if self.Asset1 is not None and self.CandleType is not None:
            yield (self.Asset1, self.CandleType)
        if self.Asset2 is not None and self.CandleType is not None:
            yield (self.Asset2, self.CandleType)

    def OnStarted(self, time):
        super(correlation_breakout_strategy, self).OnStarted(time)

        # Initialize arrays
        self._asset1_prices = [0.0] * self.LookbackPeriod
        self._asset2_prices = [0.0] * self.LookbackPeriod
        self._current_index = 0
        self._avg_correlation = 0.0
        self._last_correlation = 0.0
        self._is_initialized = False

        # Subscribe to candles for both assets
        if self.Asset1 is not None and self.Asset2 is not None and self.CandleType is not None:
            asset1_subscription = self.SubscribeCandles(self.CandleType, security=self.Asset1)
            asset2_subscription = self.SubscribeCandles(self.CandleType, security=self.Asset2)

            asset1_subscription.Bind(self.ProcessAsset1Candle).Start()
            asset2_subscription.Bind(self.ProcessAsset2Candle).Start()

            # Create chart areas if available
            area = self.CreateChartArea()
            if area is not None:
                self.DrawCandles(area, asset1_subscription)
                self.DrawCandles(area, asset2_subscription)
                self.DrawOwnTrades(area)
        else:
            self.LogWarning("Assets or candle type not specified. Strategy won't work properly.")

        # Start position protection with stop-loss
        self.StartProtection(
            takeProfit=None,
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent)
        )
    def ProcessAsset1Candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._asset1_prices[self._current_index] = float(candle.ClosePrice)
        self.CalculateCorrelation(candle)

    def ProcessAsset2Candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._asset2_prices[self._current_index] = float(candle.ClosePrice)
        self.CalculateCorrelation(candle)

    def CalculateCorrelation(self, candle):
        # We need both prices for the same bar to calculate correlation
        if self._asset1_prices[self._current_index] == 0 or self._asset2_prices[self._current_index] == 0:
            return

        # Increment index for next bar
        self._current_index = (self._current_index + 1) % self.LookbackPeriod

        # Skip calculation until we have enough data
        if not self._is_initialized and self._current_index == 0:
            self._is_initialized = True

        if not self._is_initialized:
            return

        # Calculate correlation
        self._last_correlation = self._calculate_pearson_correlation(self._asset1_prices, self._asset2_prices)

        # Process correlation through the indicator
        std_dev_value = process_float(self._corr_std_dev, self._last_correlation, candle.ServerTime, candle.State == CandleStates.Finished)

        # Move to next bar after first LookbackPeriod bars filled
        if not self._corr_std_dev.IsFormed:
            # Update running average
            self._avg_correlation = (
                self._avg_correlation * (self.LookbackPeriod - 1 if self._current_index == 0 else self._current_index - 1)
                + self._last_correlation
            ) / (self.LookbackPeriod if self._current_index == 0 else self._current_index)
            return

        # Update running average after the indicator is formed
        self._avg_correlation = (
            self._avg_correlation * (self.LookbackPeriod - 1) + self._last_correlation
        ) / self.LookbackPeriod

        # Check trading conditions
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        std_dev = to_float(std_dev_value)

        # Trading logic for correlation breakout
        if self._last_correlation < self._avg_correlation - self.Threshold * std_dev and self._get_position_value(self.Asset1) <= 0 and self._get_position_value(self.Asset2) >= 0:
            # Correlation breakdown - go long Asset1, short Asset2
            self.LogInfo(
                "Correlation breakdown: {0} < {1}".format(self._last_correlation, self._avg_correlation - self.Threshold * std_dev)
            )
            self.BuyMarket(self.Volume, self.Asset1)
            self.SellMarket(self.Volume, self.Asset2)
        elif self._last_correlation > self._avg_correlation + self.Threshold * std_dev and self._get_position_value(self.Asset1) >= 0 and self._get_position_value(self.Asset2) <= 0:
            # Correlation spike - go short Asset1, long Asset2
            self.LogInfo(
                "Correlation spike: {0} > {1}".format(self._last_correlation, self._avg_correlation + self.Threshold * std_dev)
            )
            self.SellMarket(self.Volume, self.Asset1)
            self.BuyMarket(self.Volume, self.Asset2)
        elif Math.Abs(self._last_correlation - self._avg_correlation) < 0.2 * std_dev:
            # Close position when correlation returns to average
            self.LogInfo(
                "Correlation returned to average: {0} ≈ {1}".format(self._last_correlation, self._avg_correlation)
            )

            if self._get_position_value(self.Asset1) > 0:
                self.SellMarket(Math.Abs(self._get_position_value(self.Asset1)), self.Asset1)
            if self._get_position_value(self.Asset1) < 0:
                self.BuyMarket(Math.Abs(self._get_position_value(self.Asset1)), self.Asset1)
            if self._get_position_value(self.Asset2) > 0:
                self.SellMarket(Math.Abs(self._get_position_value(self.Asset2)), self.Asset2)
            if self._get_position_value(self.Asset2) < 0:
                self.BuyMarket(Math.Abs(self._get_position_value(self.Asset2)), self.Asset2)

    def _calculate_pearson_correlation(self, x, y):
        n = len(x)
        sum_x = 0.0
        sum_y = 0.0
        sum_xy = 0.0
        sum_x2 = 0.0
        sum_y2 = 0.0

        for i in range(n):
            sum_x += x[i]
            sum_y += y[i]
            sum_xy += x[i] * y[i]
            sum_x2 += x[i] * x[i]
            sum_y2 += y[i] * y[i]

        denominator = Math.Sqrt(float(n * sum_x2 - sum_x * sum_x) * float(n * sum_y2 - sum_y * sum_y))
        if denominator == 0:
            return 0.0
        return (n * sum_xy - sum_x * sum_y) / denominator

    def _get_position_value(self, security):
        value = self.GetPositionValue(security, self.Portfolio)
        return value if value is not None else 0

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return correlation_breakout_strategy()
