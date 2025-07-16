import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")

from System import Math
from StockSharp.Messages import DataType, Level1Fields, Level1ChangeMessage, Sides, OrderTypes
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Security, Portfolio, Subscription, Order
from datatype_extensions import *

class beta_adjusted_pairs_strategy(Strategy):
    """
    Beta Adjusted Pairs Trading strategy uses beta-normalized prices
    to identify trading opportunities when the spread deviates from historical means.
    """

    def __init__(self):
        super(beta_adjusted_pairs_strategy, self).__init__()

        # Strategy parameters
        self._asset2Param = self.Param[Security]("Asset2", None) \
            .SetDisplay("Asset 2", "Secondary asset for pairs trading", "Assets") \
            .SetRequired()

        self._asset2PortfolioParam = self.Param[Portfolio]("Asset2Portfolio", None) \
            .SetDisplay("Asset 2 Portfolio", "Portfolio for trading Asset 2", "Portfolios") \
            .SetRequired()

        self._betaAsset1Param = self.Param[Security]("BetaAsset1", 1.0) \
            .SetDisplay("Beta Asset 1", "Beta coefficient for Asset 1 relative to market", "Parameters") \
            .SetNotNegative()

        self._betaAsset2Param = self.Param[Security]("BetaAsset2", 1.0) \
            .SetDisplay("Beta Asset 2", "Beta coefficient for Asset 2 relative to market", "Parameters") \
            .SetNotNegative()

        self._lookbackPeriodParam = self.Param("LookbackPeriod", 20) \
            .SetDisplay("Lookback Period", "Period for calculating spread statistics", "Parameters") \
            .SetGreaterThanZero() \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5)

        self._entryThresholdParam = self.Param("EntryThreshold", 2.0) \
            .SetDisplay("Entry Threshold", "Standard deviation threshold for entries", "Parameters") \
            .SetGreaterThanZero() \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._stopLossParam = self.Param("StopLoss", 2.0) \
            .SetDisplay("Stop Loss", "Stop loss as percentage of entry spread", "Risk Management") \
            .SetGreaterThanZero() \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 5.0, 0.5)

        # Internal state variables
        self._asset1Price = 0
        self._asset2Price = 0
        self._currentSpread = 0
        self._averageSpread = 0
        self._spreadStdDev = 0
        self._entrySpread = 0
        self._inPosition = False
        self._isLong = False  # Long = long Asset1, short Asset2; Short = short Asset1, long Asset2

        # Historical data
        self._spreadHistory = []

    @property
    def Asset1(self):
        """Asset 1 for pairs trading"""
        return self.Security

    @Asset1.setter
    def Asset1(self, value):
        self.Security = value

    @property
    def Asset2(self):
        """Asset 2 for pairs trading"""
        return self._asset2Param.Value

    @Asset2.setter
    def Asset2(self, value):
        self._asset2Param.Value = value

    @property
    def Asset1Portfolio(self):
        """Portfolio for trading Asset1"""
        return self.Portfolio

    @Asset1Portfolio.setter
    def Asset1Portfolio(self, value):
        self.Portfolio = value

    @property
    def Asset2Portfolio(self):
        """Portfolio for trading Asset2"""
        return self._asset2PortfolioParam.Value

    @Asset2Portfolio.setter
    def Asset2Portfolio(self, value):
        self._asset2PortfolioParam.Value = value

    @property
    def BetaAsset1(self):
        """Beta coefficient for Asset1 relative to market"""
        return self._betaAsset1Param.Value

    @BetaAsset1.setter
    def BetaAsset1(self, value):
        self._betaAsset1Param.Value = value

    @property
    def BetaAsset2(self):
        """Beta coefficient for Asset2 relative to market"""
        return self._betaAsset2Param.Value

    @BetaAsset2.setter
    def BetaAsset2(self, value):
        self._betaAsset2Param.Value = value

    @property
    def LookbackPeriod(self):
        """Lookback period for calculating spread statistics"""
        return self._lookbackPeriodParam.Value

    @LookbackPeriod.setter
    def LookbackPeriod(self, value):
        self._lookbackPeriodParam.Value = value

    @property
    def EntryThreshold(self):
        """Standard deviation threshold for entries (in multiples of standard deviation)"""
        return self._entryThresholdParam.Value

    @EntryThreshold.setter
    def EntryThreshold(self, value):
        self._entryThresholdParam.Value = value

    @property
    def StopLoss(self):
        """Stop loss threshold (in percentage of entry spread)"""
        return self._stopLossParam.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stopLossParam.Value = value

    def GetWorkingSecurities(self):
        """!! REQUIRED!!"""
        return [
            (self.Asset1, DataType.Level1),
            (self.Asset2, DataType.Level1)
        ]

    def OnStarted(self, time):
        super(beta_adjusted_pairs_strategy, self).OnStarted(time)

        # Verify that both assets and portfolios are set
        if self.Asset1 is None:
            raise Exception("Asset1 is not specified.")
        if self.Asset2 is None:
            raise Exception("Asset2 is not specified.")
        if self.Asset1Portfolio is None:
            raise Exception("Asset1Portfolio is not specified.")
        if self.Asset2Portfolio is None:
            raise Exception("Asset2Portfolio is not specified.")

        # Reset internal state
        self._spreadHistory = []
        self._inPosition = False
        self._currentSpread = 0
        self._averageSpread = 0
        self._spreadStdDev = 0
        self._entrySpread = 0

        # Create subscriptions for both assets
        asset1Subscription = Subscription(DataType.Level1, self.Asset1)
        asset2Subscription = Subscription(DataType.Level1, self.Asset2)

        # Handle price updates for Asset1
        self.SubscribeLevel1(asset1Subscription).Bind(self.OnAsset1Subscription).Start()

        # Handle price updates for Asset2
        self.SubscribeLevel1(asset2Subscription).Bind(self.OnAsset2Subscription).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            # If chart features are needed, add them here
            # For example, you could track and draw the spread
            pass

    def OnAsset1Subscription(self, message):
        self._asset1Price = message.TryGetDecimal(Level1Fields.LastTradePrice) or self._asset1Price
        self.UpdateSpread()

    def OnAsset2Subscription(self, message):
        self._asset2Price = message.TryGetDecimal(Level1Fields.LastTradePrice) or self._asset1Price
        self.UpdateSpread()

    def UpdateSpread(self):
        # Skip if prices are not yet available
        if self._asset1Price <= 0 or self._asset2Price <= 0:
            return

        # Calculate beta-adjusted spread
        self._currentSpread = (self._asset1Price / self.BetaAsset1) - (self._asset2Price / self.BetaAsset2)

        # Update historical spread data
        self._spreadHistory.append(self._currentSpread)

        # Keep only lookback period data points
        while len(self._spreadHistory) > self.LookbackPeriod:
            self._spreadHistory.pop(0)

        # We need at least lookback period data points to start trading
        if len(self._spreadHistory) < self.LookbackPeriod:
            return

        # Calculate spread statistics
        self.CalculateSpreadStatistics()

        # Check if we're ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Check position management
        if self._inPosition:
            self.CheckExitConditions()
        else:
            self.CheckEntryConditions()

    def CalculateSpreadStatistics(self):
        # Calculate mean
        sum_spread = 0
        for spread in self._spreadHistory:
            sum_spread += spread
        self._averageSpread = sum_spread / len(self._spreadHistory)

        # Calculate standard deviation
        sum_of_sq_diff = 0
        for spread in self._spreadHistory:
            difference = spread - self._averageSpread
            sum_of_sq_diff += difference * difference
        self._spreadStdDev = Math.Sqrt(sum_of_sq_diff / len(self._spreadHistory))

    def CheckEntryConditions(self):
        # Make sure we have valid statistics
        if self._spreadStdDev == 0:
            return

        # Normalized spread distance from mean (in standard deviations)
        zScore = (self._currentSpread - self._averageSpread) / self._spreadStdDev

        # Check if spread is significantly above average (short signal)
        if zScore > self.EntryThreshold:
            self.EnterShortPosition()
        # Check if spread is significantly below average (long signal)
        elif zScore < -self.EntryThreshold:
            self.EnterLongPosition()

    def CheckExitConditions(self):
        # Check for mean reversion (exit condition)
        if self._isLong and self._currentSpread > self._averageSpread:
            self.ExitPosition()
        elif not self._isLong and self._currentSpread < self._averageSpread:
            self.ExitPosition()
        # Check for stop loss
        else:
            spreadDifference = abs(self._currentSpread - self._entrySpread)
            stopLossThreshold = self._entrySpread * self.StopLoss / 100
            if spreadDifference > stopLossThreshold:
                self.ExitPosition()
                self.LogInfo("Stop loss triggered. Entry spread: {0}, Current spread: {1}".format(
                    self._entrySpread, self._currentSpread))

    def EnterLongPosition(self):
        # Long = long Asset1, short Asset2
        self._inPosition = True
        self._isLong = True
        self._entrySpread = self._currentSpread

        # Calculate trade volume based on strategy's Volume property
        volume = self.Volume

        # Create and register orders
        longOrder = Order()
        longOrder.Portfolio = self.Asset1Portfolio
        longOrder.Security = self.Asset1
        longOrder.Side = Sides.Buy
        longOrder.Volume = volume
        longOrder.Type = OrderTypes.Market

        shortOrder = Order()
        shortOrder.Portfolio = self.Asset2Portfolio
        shortOrder.Security = self.Asset2
        shortOrder.Side = Sides.Sell
        shortOrder.Volume = volume
        shortOrder.Type = OrderTypes.Market

        self.RegisterOrder(longOrder)
        self.RegisterOrder(shortOrder)

        self.LogInfo("Entered LONG position (long Asset1, short Asset2) at spread: {0}, Mean: {1}, StdDev: {2}".format(
            self._currentSpread, self._averageSpread, self._spreadStdDev))

    def EnterShortPosition(self):
        # Short = short Asset1, long Asset2
        self._inPosition = True
        self._isLong = False
        self._entrySpread = self._currentSpread

        # Calculate trade volume based on strategy's Volume property
        volume = self.Volume

        # Create and register orders
        shortOrder = Order()
        shortOrder.Portfolio = self.Asset1Portfolio
        shortOrder.Security = self.Asset1
        shortOrder.Side = Sides.Sell
        shortOrder.Volume = volume
        shortOrder.Type = OrderTypes.Market

        longOrder = Order()
        longOrder.Portfolio = self.Asset2Portfolio
        longOrder.Security = self.Asset2
        longOrder.Side = Sides.Buy
        longOrder.Volume = volume
        longOrder.Type = OrderTypes.Market

        self.RegisterOrder(shortOrder)
        self.RegisterOrder(longOrder)

        self.LogInfo("Entered SHORT position (short Asset1, long Asset2) at spread: {0}, Mean: {1}, StdDev: {2}".format(
            self._currentSpread, self._averageSpread, self._spreadStdDev))

    def ExitPosition(self):
        if not self._inPosition:
            return

        volume = self.Volume

        if self._isLong:
            # Close long Asset1, short Asset2
            closeAsset1 = Order()
            closeAsset1.Portfolio = self.Asset1Portfolio
            closeAsset1.Security = self.Asset1
            closeAsset1.Side = Sides.Sell
            closeAsset1.Volume = volume
            closeAsset1.Type = OrderTypes.Market

            closeAsset2 = Order()
            closeAsset2.Portfolio = self.Asset2Portfolio
            closeAsset2.Security = self.Asset2
            closeAsset2.Side = Sides.Buy
            closeAsset2.Volume = volume
            closeAsset2.Type = OrderTypes.Market

            self.RegisterOrder(closeAsset1)
            self.RegisterOrder(closeAsset2)
        else:
            # Close short Asset1, long Asset2
            closeAsset1 = Order()
            closeAsset1.Portfolio = self.Asset1Portfolio
            closeAsset1.Security = self.Asset1
            closeAsset1.Side = Sides.Buy
            closeAsset1.Volume = volume
            closeAsset1.Type = OrderTypes.Market

            closeAsset2 = Order()
            closeAsset2.Portfolio = self.Asset2Portfolio
            closeAsset2.Security = self.Asset2
            closeAsset2.Side = Sides.Sell
            closeAsset2.Volume = volume
            closeAsset2.Type = OrderTypes.Market

            self.RegisterOrder(closeAsset1)
            self.RegisterOrder(closeAsset2)

        self._inPosition = False
        self.LogInfo("Exited position at spread: {0}, Entry spread: {1}".format(
            self._currentSpread, self._entrySpread))

    def OnStopped(self):
        # Close any open position when strategy stops
        if self._inPosition:
            self.ExitPosition()
        super(beta_adjusted_pairs_strategy, self).OnStopped()

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return beta_adjusted_pairs_strategy()
