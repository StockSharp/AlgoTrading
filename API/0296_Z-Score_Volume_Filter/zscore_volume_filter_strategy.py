import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class zscore_volume_filter_strategy(Strategy):
    """
    Z-Score with Volume Filter strategy.
    Trading based on Z-score (standard deviations from the mean) with volume confirmation.
    """

    def __init__(self):
        super(zscore_volume_filter_strategy, self).__init__()

        # Initialize strategy parameters
        self._lookback_period = self.Param("LookbackPeriod", 20) \
            .SetRange(10, 50) \
            .SetDisplay("Lookback Period", "Period for calculating moving averages and standard deviation", "Parameters") \
            .SetCanOptimize(True)

        self._z_score_threshold = self.Param("ZScoreThreshold", 2.0) \
            .SetRange(1.0, 3.0) \
            .SetDisplay("Z-Score Threshold", "Z-Score threshold for entry signals", "Parameters") \
            .SetCanOptimize(True)

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetRange(0.5, 5.0) \
            .SetDisplay("Stop Loss", "Stop loss percentage from entry price", "Parameters") \
            .SetCanOptimize(True)

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Timeframe for candles", "Parameters")

        # Technical indicators
        self._price_sma = None
        self._price_std_dev = None
        self._volume_sma = None

        # Current data values
        self._current_price = 0.0
        self._current_volume = 0.0
        self._average_price = 0.0
        self._price_std_deviation = 0.0
        self._average_volume = 0.0

    @property
    def LookbackPeriod(self):
        """Lookback period for calculating moving averages and standard deviation."""
        return self._lookback_period.Value

    @LookbackPeriod.setter
    def LookbackPeriod(self, value):
        self._lookback_period.Value = value

    @property
    def ZScoreThreshold(self):
        """Z-Score threshold for entry signals."""
        return self._z_score_threshold.Value

    @ZScoreThreshold.setter
    def ZScoreThreshold(self, value):
        self._z_score_threshold.Value = value

    @property
    def StopLossPercent(self):
        """Stop loss percentage from entry price."""
        return self._stop_loss_percent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def CandleType(self):
        """Candle timeframe type for data subscription."""
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        """Called when the strategy starts."""
        super(zscore_volume_filter_strategy, self).OnStarted(time)

        if self.Security is None:
            raise Exception("Security is not specified.")

        self._current_price = 0.0
        self._current_volume = 0.0
        self._average_price = 0.0
        self._price_std_deviation = 0.0
        self._average_volume = 0.0

        # Initialize indicators
        self._price_sma = SimpleMovingAverage()
        self._price_sma.Length = self.LookbackPeriod
        self._price_std_dev = StandardDeviation()
        self._price_std_dev.Length = self.LookbackPeriod
        self._volume_sma = SimpleMovingAverage()
        self._volume_sma.Length = self.LookbackPeriod

        # Set up candle subscription
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind indicators to process data
        subscription.BindEx(self._price_sma, self._price_std_dev, self._volume_sma, self.ProcessCandle).Start()

        # Setup visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._price_sma)
            self.DrawOwnTrades(area)

        # Setup position protection
        self.StartProtection(
            Unit(0, UnitTypes.Absolute),  # No take profit
            Unit(self.StopLossPercent, UnitTypes.Percent),  # Stop loss in percent
            False  # No trailing stop
        )

    def ProcessCandle(self, candle, price_sma_value, price_std_dev_value, volume_sma_value):
        """Process candle and compute indicator values."""
        if candle.State != CandleStates.Finished:
            return

        # Store current values
        self._current_price = candle.ClosePrice
        self._current_volume = candle.TotalVolume

        # Process indicators
        self._average_price = float(price_sma_value)
        self._price_std_deviation = float(price_std_dev_value)
        self._average_volume = float(volume_sma_value)

        # Check trading signals
        self.CheckSignal()

    def CheckSignal(self):
        # Ensure strategy is ready for trading and indicators are formed
        if (not self.IsFormedAndOnlineAndAllowTrading() or
                not self._price_sma.IsFormed or
                not self._price_std_dev.IsFormed or
                not self._volume_sma.IsFormed):
            return

        # Calculate Z-score (price in standard deviations from mean)
        z_score = (self._current_price - self._average_price) / self._price_std_deviation

        # Check volume filter - require above average volume for confirmation
        is_high_volume = self._current_volume > self._average_volume

        # If we have no position, check for entry signals
        if self.Position == 0:
            # Long signal: price is below threshold (undervalued) with high volume
            if z_score < -self.ZScoreThreshold and is_high_volume:
                self.BuyMarket(self.Volume)
                self.LogInfo("LONG: Z-Score: {0:F2}, Volume: High".format(z_score))
            # Short signal: price is above threshold (overvalued) with high volume
            elif z_score > self.ZScoreThreshold and is_high_volume:
                self.SellMarket(self.Volume)
                self.LogInfo("SHORT: Z-Score: {0:F2}, Volume: High".format(z_score))
        # Check for exit signals
        else:
            # Exit when price returns to mean
            if (self.Position > 0 and z_score >= 0) or (self.Position < 0 and z_score <= 0):
                self.ClosePosition()
                self.LogInfo("CLOSE: Z-Score: {0:F2}".format(z_score))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return zscore_volume_filter_strategy()
