import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import Momentum, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class seasonality_adjusted_momentum_strategy(Strategy):
    """
    Strategy based on momentum indicator adjusted with seasonality strength.
    """

    def __init__(self):
        # Initialize a new instance of SeasonalityAdjustedMomentumStrategy.
        super(seasonality_adjusted_momentum_strategy, self).__init__()

        # Period for Momentum indicator.
        self._momentum_period = self.Param("MomentumPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("Momentum Period", "Period for momentum indicator", "Strategy Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(7, 21, 7)

        # Threshold for seasonality strength.
        self._seasonality_threshold = self.Param("SeasonalityThreshold", 0.5) \
            .SetGreaterThanZero() \
            .SetDisplay("Seasonality Threshold", "Threshold value for seasonality strength", "Strategy Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(0.3, 0.7, 0.1)

        # Stop loss percentage.
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Strategy Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        # Candle type parameter.
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromDays(1))) \
            .SetDisplay("Candle Type", "Type of candles for strategy", "General")

        # Dictionary to store seasonality strength values for each month
        self._seasonal_strength_by_month = {}

        # Initialize seasonality strength for each month (example data)
        self.InitializeSeasonalityData()

    @property
    def MomentumPeriod(self):
        return self._momentum_period.Value

    @MomentumPeriod.setter
    def MomentumPeriod(self, value):
        self._momentum_period.Value = value

    @property
    def SeasonalityThreshold(self):
        return self._seasonality_threshold.Value

    @SeasonalityThreshold.setter
    def SeasonalityThreshold(self, value):
        self._seasonality_threshold.Value = value

    @property
    def StopLossPercent(self):
        return self._stop_loss_percent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def InitializeSeasonalityData(self):
        # This is sample data - in a real strategy, this would be calculated from historical data
        # Positive values indicate historically strong months, negative values indicate weak months
        self._seasonal_strength_by_month[1] = 0.8  # January
        self._seasonal_strength_by_month[2] = 0.2  # February
        self._seasonal_strength_by_month[3] = 0.5  # March
        self._seasonal_strength_by_month[4] = 0.7  # April
        self._seasonal_strength_by_month[5] = 0.3  # May
        self._seasonal_strength_by_month[6] = -0.2  # June
        self._seasonal_strength_by_month[7] = -0.3  # July
        self._seasonal_strength_by_month[8] = -0.4  # August
        self._seasonal_strength_by_month[9] = -0.7  # September
        self._seasonal_strength_by_month[10] = 0.4  # October
        self._seasonal_strength_by_month[11] = 0.6  # November
        self._seasonal_strength_by_month[12] = 0.9  # December

    def OnStarted(self, time):
        super(seasonality_adjusted_momentum_strategy, self).OnStarted(time)

        # Create indicators
        momentum = Momentum()
        momentum.Length = self.MomentumPeriod
        momentumAvg = SimpleMovingAverage()
        momentumAvg.Length = self.MomentumPeriod

        # Create subscription
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind indicators to subscription
        subscription.Bind(momentum, momentumAvg, self.ProcessCandle).Start()

        # Enable position protection with percentage stop-loss
        self.StartProtection(
            takeProfit=Unit(0),  # We'll handle exits in the strategy logic
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent),
            useMarketOrders=True
        )

        # Setup chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, momentum)
            self.DrawIndicator(area, momentumAvg)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, momentum_value, momentum_avg_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Get current month
        current_month = candle.OpenTime.Month

        # Get seasonality strength for current month
        seasonal_strength = 0
        if current_month in self._seasonal_strength_by_month:
            seasonal_strength = self._seasonal_strength_by_month[current_month]

        # Log seasonality data
        self.LogInfo(f"Month: {current_month}, Seasonality Strength: {seasonal_strength}, Momentum: {momentum_value}, Avg Momentum: {momentum_avg_value}")

        # Define entry conditions with seasonality adjustment
        longEntryCondition = momentum_value > momentum_avg_value and \
                             seasonal_strength > self.SeasonalityThreshold and \
                             self.Position <= 0

        shortEntryCondition = momentum_value < momentum_avg_value and \
                              seasonal_strength < -self.SeasonalityThreshold and \
                              self.Position >= 0

        # Define exit conditions
        longExitCondition = momentum_value < momentum_avg_value and self.Position > 0
        shortExitCondition = momentum_value > momentum_avg_value and self.Position < 0

        # Execute trading logic
        if longEntryCondition:
            # Calculate position size
            positionSize = self.Volume + Math.Abs(self.Position)

            # Enter long position
            self.BuyMarket(positionSize)

            self.LogInfo(f"Long entry: Price={candle.ClosePrice}, Momentum={momentum_value}, Avg={momentum_avg_value}, Seasonality={seasonal_strength}")
        elif shortEntryCondition:
            # Calculate position size
            positionSize = self.Volume + Math.Abs(self.Position)

            # Enter short position
            self.SellMarket(positionSize)

            self.LogInfo(f"Short entry: Price={candle.ClosePrice}, Momentum={momentum_value}, Avg={momentum_avg_value}, Seasonality={seasonal_strength}")
        elif longExitCondition:
            # Exit long position
            self.SellMarket(Math.Abs(self.Position))
            self.LogInfo(f"Long exit: Price={candle.ClosePrice}, Momentum={momentum_value}, Avg={momentum_avg_value}")
        elif shortExitCondition:
            # Exit short position
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo(f"Short exit: Price={candle.ClosePrice}, Momentum={momentum_value}, Avg={momentum_avg_value}")

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return seasonality_adjusted_momentum_strategy()
