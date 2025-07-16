import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageTrueRange, StandardDeviation
from StockSharp.Algo.Strategies import Strategy

class volatility_adjusted_mean_reversion_strategy(Strategy):
    """
    Volatility Adjusted Mean Reversion strategy.
    Uses ATR and Standard Deviation to create adaptive entry thresholds.

    See more examples: https://github.com/StockSharp/AlgoTrading
    """

    def __init__(self):
        super(volatility_adjusted_mean_reversion_strategy, self).__init__()

        # Period for indicators.
        self._period = self.Param("Period", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Period", "Period for indicators", "Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 10)

        # Multiplier for entry threshold.
        self._multiplier = self.Param("Multiplier", 2.0) \
            .SetRange(0.1, 1e6) \
            .SetDisplay("Multiplier", "Multiplier for entry threshold", "Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        # Candle type for strategy.
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle type for strategy", "Common")

    @property
    def period(self):
        """Period for indicators."""
        return self._period.Value

    @period.setter
    def period(self, value):
        self._period.Value = value

    @property
    def multiplier(self):
        """Multiplier for entry threshold."""
        return self._multiplier.Value

    @multiplier.setter
    def multiplier(self, value):
        self._multiplier.Value = value

    @property
    def candle_type(self):
        """Candle type for strategy."""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnStarted(self, time):
        super(volatility_adjusted_mean_reversion_strategy, self).OnStarted(time)

        # Create indicators
        sma = SimpleMovingAverage()
        sma.Length = self.period
        atr = AverageTrueRange()
        atr.Length = self.period
        std_dev = StandardDeviation()
        std_dev.Length = self.period

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.candle_type)

        # First, bind SMA and ATR
        subscription.Bind(sma, atr, std_dev, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawIndicator(area, atr)
            self.DrawOwnTrades(area)

        # Enable position protection
        self.StartProtection(
            Unit(0, UnitTypes.Absolute),  # No take profit
            Unit(2, UnitTypes.Absolute)   # Stop loss at 2*ATR
        )

    def ProcessCandle(self, candle, sma_value, atr_value, std_dev_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Skip if standard deviation is too small to avoid division by zero
        if std_dev_value < 0.0001:
            return

        # Calculate volatility ratio
        volatility_ratio = atr_value / std_dev_value

        # Calculate volatility-adjusted thresholds
        threshold = self.multiplier * atr_value / volatility_ratio
        upper_threshold = sma_value + threshold
        lower_threshold = sma_value - threshold

        # Long setup - price below lower threshold
        if candle.ClosePrice < lower_threshold and self.Position <= 0:
            # Buy signal - price has deviated too much below average
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        # Short setup - price above upper threshold
        elif candle.ClosePrice > upper_threshold and self.Position >= 0:
            # Sell signal - price has deviated too much above average
            self.SellMarket(self.Volume + Math.Abs(self.Position))
        # Exit long position when price returns to average
        elif self.Position > 0 and candle.ClosePrice >= sma_value:
            # Close long position
            self.SellMarket(self.Position)
        # Exit short position when price returns to average
        elif self.Position < 0 and candle.ClosePrice <= sma_value:
            # Close short position
            self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return volatility_adjusted_mean_reversion_strategy()

