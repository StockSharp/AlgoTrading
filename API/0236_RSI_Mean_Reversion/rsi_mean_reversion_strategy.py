import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, UnitTypes, Unit, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class rsi_mean_reversion_strategy(Strategy):
    """
    RSI Mean Reversion Strategy.
    Enter when RSI deviates from its average by a certain multiple of standard deviation.
    Exit when RSI returns to its average.
    """

    def __init__(self):
        super(rsi_mean_reversion_strategy, self).__init__()

        # Initialize strategy parameters
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "Period for RSI calculation", "Strategy Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 20, 2)

        self._average_period = self.Param("AveragePeriod", 20) \
            .SetDisplay("Average Period", "Period for RSI average calculation", "Strategy Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 30, 5)

        self._multiplier = self.Param("Multiplier", 2.0) \
            .SetDisplay("StdDev Multiplier", "Standard deviation multiplier for entry", "Strategy Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "Strategy Parameters")

        # Internal indicators
        self._rsi = None
        self._rsi_average = None
        self._rsi_std_dev = None
        self._prev_rsi_value = 0

    @property
    def RsiPeriod(self):
        """RSI period."""
        return self._rsi_period.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsi_period.Value = value

    @property
    def AveragePeriod(self):
        """Period for RSI average calculation."""
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
        """Called when the strategy starts."""
        super(rsi_mean_reversion_strategy, self).OnStarted(time)

        self._prev_rsi_value = 0

        # Create indicators
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.RsiPeriod
        self._rsi_average = SimpleMovingAverage()
        self._rsi_average.Length = self.AveragePeriod
        self._rsi_std_dev = StandardDeviation()
        self._rsi_std_dev.Length = self.AveragePeriod

        # Create candle subscription
        subscription = self.SubscribeCandles(self.CandleType)

        # Define custom indicator chain processing
        subscription.Bind(self._rsi, self.ProcessRsi).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._rsi)
            self.DrawOwnTrades(area)

        # Enable position protection
        self.StartProtection(
            takeProfit=Unit(5, UnitTypes.Percent),
            stopLoss=Unit(2, UnitTypes.Percent)
        )
    def ProcessRsi(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        # Process RSI through average and standard deviation indicators
        rsi_avg_value = to_float(process_float(self._rsi_average, rsi_value, candle.ServerTime, candle.State == CandleStates.Finished))
        rsi_std_dev_value = to_float(process_float(self._rsi_std_dev, rsi_value, candle.ServerTime, candle.State == CandleStates.Finished))

        # Store previous RSI value for changes detection
        current_rsi_value = rsi_value

        # Check if strategy is ready for trading
        if not self.IsFormedAndOnlineAndAllowTrading() or not self._rsi_average.IsFormed or not self._rsi_std_dev.IsFormed:
            self._prev_rsi_value = current_rsi_value
            return

        # Calculate bands
        upper_band = rsi_avg_value + self.Multiplier * rsi_std_dev_value
        lower_band = rsi_avg_value - self.Multiplier * rsi_std_dev_value

        self.LogInfo("RSI: {0}, RSI Avg: {1}, Upper: {2}, Lower: {3}".format(
            current_rsi_value, rsi_avg_value, upper_band, lower_band))

        # Entry logic
        if self.Position == 0:
            # Long Entry: RSI is below lower band
            if current_rsi_value < lower_band:
                self.LogInfo("Buy Signal - RSI ({0}) < Lower Band ({1})".format(current_rsi_value, lower_band))
                self.BuyMarket(self.Volume)
            # Short Entry: RSI is above upper band
            elif current_rsi_value > upper_band:
                self.LogInfo("Sell Signal - RSI ({0}) > Upper Band ({1})".format(current_rsi_value, upper_band))
                self.SellMarket(self.Volume)
        # Exit logic
        elif self.Position > 0 and current_rsi_value > rsi_avg_value:
            # Exit Long: RSI returned to average
            self.LogInfo("Exit Long - RSI ({0}) > RSI Avg ({1})".format(current_rsi_value, rsi_avg_value))
            self.SellMarket(Math.Abs(self.Position))
        elif self.Position < 0 and current_rsi_value < rsi_avg_value:
            # Exit Short: RSI returned to average
            self.LogInfo("Exit Short - RSI ({0}) < RSI Avg ({1})".format(current_rsi_value, rsi_avg_value))
            self.BuyMarket(Math.Abs(self.Position))

        self._prev_rsi_value = current_rsi_value

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return rsi_mean_reversion_strategy()
