import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, UnitTypes, Unit
from StockSharp.Algo.Indicators import OnBalanceVolume, SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy

class obv_mean_reversion_strategy(Strategy):
    """
    OBV Mean Reversion Strategy (244).
    Enter when OBV deviates from its average by a certain multiple of standard deviation.
    Exit when OBV returns to its average.

    See more examples: https://github.com/StockSharp/AlgoTrading
    """

    def __init__(self):
        super(obv_mean_reversion_strategy, self).__init__()

        # Initialize strategy parameters
        self._average_period = self.Param("AveragePeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Average Period", "Period for OBV average calculation", "Strategy Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 30, 5)

        self._multiplier = self.Param("Multiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("StdDev Multiplier", "Standard deviation multiplier for entry", "Strategy Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "Strategy Parameters")

        # Internal fields
        self._obv = None
        self._obv_average = None
        self._obv_std_dev = None

        self._current_obv = None
        self._obv_avg_value = None
        self._obv_std_dev_value = None

    @property
    def AveragePeriod(self):
        """Period for OBV average calculation."""
        return self._average_period.Value

    @AveragePeriod.setter
    def AveragePeriod(self, value):
        self._average_period.Value = value

    @property
    def Multiplier(self):
        """Standard deviation multiplier for entry."""
        return self._multiplier.Value

    @Multiplier.setter
    def Multiplier(self, value):
        self._multiplier.Value = value

    @property
    def CandleType(self):
        """Type of candles to use."""
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(obv_mean_reversion_strategy, self).OnStarted(time)

        self._current_obv = None
        self._obv_avg_value = None
        self._obv_std_dev_value = None

        # Create indicators
        self._obv = OnBalanceVolume()
        self._obv_average = SimpleMovingAverage()
        self._obv_average.Length = self.AveragePeriod
        self._obv_std_dev = StandardDeviation()
        self._obv_std_dev.Length = self.AveragePeriod

        # Create candle subscription
        subscription = self.SubscribeCandles(self.CandleType)

        # Create processing chain
        subscription.BindEx(self._obv, self.ProcessObv).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._obv)
            self.DrawOwnTrades(area)

        # Enable position protection
        self.StartProtection(
            takeProfit=Unit(5, UnitTypes.Percent),
            stopLoss=Unit(2, UnitTypes.Percent)
        )

    def ProcessObv(self, candle, obv_value):
        if candle.State != CandleStates.Finished:
            return

        # Extract OBV value
        self._current_obv = float(obv_value)

        # Process OBV through average and standard deviation indicators
        avg_indicator_value = self._obv_average.Process(obv_value)
        std_dev_indicator_value = self._obv_std_dev.Process(obv_value)

        self._obv_avg_value = float(avg_indicator_value)
        self._obv_std_dev_value = float(std_dev_indicator_value)

        # Check if strategy is ready for trading
        if not self.IsFormedAndOnlineAndAllowTrading() or not self._obv_average.IsFormed or not self._obv_std_dev.IsFormed:
            return

        # Ensure we have all needed values
        if self._current_obv is None or self._obv_avg_value is None or self._obv_std_dev_value is None:
            return

        # Calculate bands
        upper_band = self._obv_avg_value + self.Multiplier * self._obv_std_dev_value
        lower_band = self._obv_avg_value - self.Multiplier * self._obv_std_dev_value

        self.LogInfo("OBV: {0}, OBV Avg: {1}, Upper: {2}, Lower: {3}".format(
            self._current_obv, self._obv_avg_value, upper_band, lower_band))

        # Entry logic
        if self.Position == 0:
            # Long Entry: OBV is below lower band (OBV oversold)
            if self._current_obv < lower_band:
                self.LogInfo("Buy Signal - OBV ({0}) < Lower Band ({1})".format(self._current_obv, lower_band))
                self.BuyMarket(self.Volume)
            # Short Entry: OBV is above upper band (OBV overbought)
            elif self._current_obv > upper_band:
                self.LogInfo("Sell Signal - OBV ({0}) > Upper Band ({1})".format(self._current_obv, upper_band))
                self.SellMarket(self.Volume)
        # Exit logic
        elif self.Position > 0 and self._current_obv > self._obv_avg_value:
            # Exit Long: OBV returned to average
            self.LogInfo("Exit Long - OBV ({0}) > OBV Avg ({1})".format(self._current_obv, self._obv_avg_value))
            self.SellMarket(Math.Abs(self.Position))
        elif self.Position < 0 and self._current_obv < self._obv_avg_value:
            # Exit Short: OBV returned to average
            self.LogInfo("Exit Short - OBV ({0}) < OBV Avg ({1})".format(self._current_obv, self._obv_avg_value))
            self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return obv_mean_reversion_strategy()
