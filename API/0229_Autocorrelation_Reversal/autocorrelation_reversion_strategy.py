import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from StockSharp.BusinessEntities import Security


class autocorrelation_reversion_strategy(Strategy):
    """
    Strategy that trades based on price autocorrelation.
    Buys when autocorrelation is negative and price is below average.
    Sells when autocorrelation is negative and price is above average.

    See more examples: https://github.com/StockSharp/AlgoTrading
    """

    def __init__(self):
        super(autocorrelation_reversion_strategy, self).__init__()

        # Initialize strategy parameters
        self._auto_corr_period = self.Param("AutoCorrPeriod", 20) \
            .SetDisplay("Autocorrelation period", "Period for autocorrelation calculation", "Strategy parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 30, 5)

        self._auto_corr_threshold = self.Param("AutoCorrThreshold", -0.3) \
            .SetDisplay("Autocorr threshold", "Threshold for autocorrelation signals", "Strategy parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(-0.5, -0.1, 0.1)

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop-loss %", "Stop-loss as percentage from entry price", "Risk management") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle type", "Type of candles to use", "General")

        self._sma = None
        self._current_price = 0.0
        self._price_history = []
        self._latest_autocorrelation = 0.0

    # Period for autocorrelation calculation.
    @property
    def AutoCorrPeriod(self):
        return self._auto_corr_period.Value

    @AutoCorrPeriod.setter
    def AutoCorrPeriod(self, value):
        self._auto_corr_period.Value = value

    # Autocorrelation threshold for signal generation.
    @property
    def AutoCorrThreshold(self):
        return self._auto_corr_threshold.Value

    @AutoCorrThreshold.setter
    def AutoCorrThreshold(self, value):
        self._auto_corr_threshold.Value = value

    # Stop-loss percentage.
    @property
    def StopLossPercent(self):
        return self._stop_loss_percent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stop_loss_percent.Value = value

    # Type of candles to use.
    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        super(autocorrelation_reversion_strategy, self).OnStarted(time)

        self._price_history = []
        self._latest_autocorrelation = 0.0
        self._current_price = 0.0

        # Initialize the SMA indicator (using same period as autocorrelation for simplicity)
        self._sma = SimpleMovingAverage()
        self._sma.Length = self.AutoCorrPeriod

        # Create a subscription to candlesticks
        subscription = self.SubscribeCandles(self.CandleType)

        # Subscribe to candle processing
        subscription.Bind(self._sma, self.ProcessCandle).Start()

        # Start position protection
        self.StartProtection(
            Unit(self.StopLossPercent, UnitTypes.Percent),
            Unit(self.StopLossPercent * 1.5, UnitTypes.Percent)
        )

        # Setup chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._sma)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, sma_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Update current price and price history
        self._current_price = candle.ClosePrice

        # Update price history queue
        self._price_history.append(self._current_price)
        if len(self._price_history) > self.AutoCorrPeriod:
            self._price_history.pop(0)

        # Wait until we have enough data
        if len(self._price_history) < self.AutoCorrPeriod:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Calculate autocorrelation
        self._latest_autocorrelation = self.CalculateAutocorrelation()

        # Log the autocorrelation value
        self.LogInfo(
            "Autocorrelation: {0}, Current price: {1}, SMA: {2}".format(
                self._latest_autocorrelation, self._current_price, sma_value))

        # Trading logic: Look for negative autocorrelation below threshold
        if self._latest_autocorrelation < self.AutoCorrThreshold:
            # Price below average - buy signal
            if self._current_price < sma_value and self.Position <= 0:
                self.BuyMarket(self.Volume)
                self.LogInfo(
                    "Buy signal: Autocorr={0}, Price={1}, SMA={2}".format(
                        self._latest_autocorrelation, self._current_price, sma_value))
            # Price above average - sell signal
            elif self._current_price > sma_value and self.Position >= 0:
                self.SellMarket(self.Volume + Math.Abs(self.Position))
                self.LogInfo(
                    "Sell signal: Autocorr={0}, Price={1}, SMA={2}".format(
                        self._latest_autocorrelation, self._current_price, sma_value))

    def CalculateAutocorrelation(self):
        # Convert queue to array for easier calculation
        prices = list(self._price_history)

        # Calculate price changes
        price_changes = [prices[i + 1] - prices[i] for i in range(len(prices) - 1)]

        # Calculate autocorrelation of lag 1
        if not price_changes:
            return 0.0
        mean_change = sum(price_changes) / len(price_changes)

        numerator = 0.0
        denominator = 0.0

        for i in range(len(price_changes) - 1):
            deviation1 = price_changes[i] - mean_change
            deviation2 = price_changes[i + 1] - mean_change

            numerator += deviation1 * deviation2
            denominator += deviation1 * deviation1

        # Guard against division by zero
        if denominator == 0:
            return 0.0

        return numerator / denominator

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return autocorrelation_reversion_strategy()
