import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Math
from System.Collections.Generic import Queue
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates, Sides
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Order, Security
from datatype_extensions import *

class cointegration_pairs_strategy(Strategy):
    """
    Cointegration pairs trading strategy.
    Trades based on cointegration relationship between two assets.
    """

    def __init__(self):
        super(cointegration_pairs_strategy, self).__init__()

        # Period for calculation of residual mean and standard deviation.
        self._period = self.Param("Period", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Period", "Period for residual calculations", "Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 10)

        # Entry threshold as a multiple of standard deviation.
        self._entryThreshold = self.Param("EntryThreshold", 2.0) \
            .SetRange(0.1, 100.0) \
            .SetDisplay("Entry Threshold", "Entry threshold as multiple of standard deviation", "Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        # Beta coefficient for calculation of residual.
        self._beta = self.Param("Beta", 1.0) \
            .SetRange(0.01, 10.0) \
            .SetDisplay("Beta", "Coefficient of cointegration", "Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(0.5, 2.0, 0.1)

        # Second asset for pair trading.
        self._asset2 = self.Param[Security]("Asset2", None) \
            .SetDisplay("Asset 2", "Second asset for pair trading", "Parameters")

        # Stop loss percentage.
        self._stopLossPercent = self.Param("StopLossPercent", 2.0) \
            .SetRange(0.1, 100.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 5.0, 1.0)

        # Candle type for strategy.
        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Candle type for strategy", "Common")

        # Internal state
        self._residualMean = 0.0
        self._residualStdDev = 0.0
        self._residualSum = 0.0
        self._squaredResidualSum = 0.0
        self._residuals = Queue[float]()
        self._asset1Price = 0.0
        self._asset2Price = 0.0
        self._asset2Portfolio = None

    @property
    def Period(self):
        return self._period.Value

    @Period.setter
    def Period(self, value):
        self._period.Value = value

    @property
    def EntryThreshold(self):
        return self._entryThreshold.Value

    @EntryThreshold.setter
    def EntryThreshold(self, value):
        self._entryThreshold.Value = value

    @property
    def Beta(self):
        return self._beta.Value

    @Beta.setter
    def Beta(self, value):
        self._beta.Value = value

    @property
    def Asset2(self):
        return self._asset2.Value

    @Asset2.setter
    def Asset2(self, value):
        self._asset2.Value = value

    @property
    def StopLossPercent(self):
        return self._stopLossPercent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stopLossPercent.Value = value

    @property
    def CandleType(self):
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType), (self.Asset2, self.CandleType)]

    def OnReseted(self):
        super(cointegration_pairs_strategy, self).OnReseted()

        self._residualMean = 0
        self._residualStdDev = 0
        self._residualSum = 0
        self._squaredResidualSum = 0
        self._residuals.Clear()
        self._asset1Price = 0
        self._asset2Price = 0

    def OnStarted(self, time):
        super(cointegration_pairs_strategy, self).OnStarted(time)

        if self.Asset2 is None:
            raise Exception("Second asset is not specified.")

        # Use the same portfolio for second asset or find another portfolio
        self._asset2Portfolio = self.Portfolio


        # Create subscriptions for both assets
        asset1Subscription = self.SubscribeCandles(self.CandleType)
        asset2Subscription = self.SubscribeCandles(self.CandleType, self.Asset2)

        # Subscribe to Asset1 candles
        asset1Subscription.Bind(self.ProcessAsset1Candle).Start()

        # Subscribe to Asset2 candles
        asset2Subscription.Bind(self.ProcessAsset2Candle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, asset1Subscription)
            self.DrawOwnTrades(area)

        # Enable position protection with stop loss
        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent)
        )
    def ProcessAsset1Candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._asset1Price = float(candle.ClosePrice)
        self.ProcessPair()

    def ProcessAsset2Candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._asset2Price = float(candle.ClosePrice)
        self.ProcessPair()

    def ProcessPair(self):
        if self._asset1Price == 0 or self._asset2Price == 0:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Calculate residual = Asset1Price - Beta * Asset2Price
        residual = self._asset1Price - self.Beta * self._asset2Price

        # Track residual statistics over period
        self._residuals.Enqueue(residual)
        self._residualSum += residual
        self._squaredResidualSum += residual * residual

        if self._residuals.Count > self.Period:
            oldResidual = self._residuals.Dequeue()
            self._residualSum -= oldResidual
            self._squaredResidualSum -= oldResidual * oldResidual

        if self._residuals.Count == self.Period:
            # Calculate mean and standard deviation
            self._residualMean = self._residualSum / self.Period
            variance = (self._squaredResidualSum / self.Period) - (self._residualMean * self._residualMean)
            self._residualStdDev = 0.0001 if variance <= 0 else Math.Sqrt(float(variance))

            # Calculate z-score of current residual
            zScore = 0 if self._residualStdDev == 0 else (residual - self._residualMean) / self._residualStdDev

            # Check for trading signals
            if zScore < -self.EntryThreshold and self.Position <= 0:
                # Long Asset1, Short Asset2
                # First, close any existing short position on Asset1
                self.BuyMarket(self.Volume + Math.Abs(self.Position))

                # Then, short Asset2 using the second portfolio
                if self._asset2Portfolio is not None:
                    asset2Order = Order()
                    asset2Order.Side = Sides.Sell
                    asset2Order.Security = self.Asset2
                    asset2Order.Portfolio = self._asset2Portfolio
                    asset2Order.Volume = self.Volume * self.Beta
                    self.RegisterOrder(asset2Order)

            elif zScore > self.EntryThreshold and self.Position >= 0:
                # Short Asset1, Long Asset2
                # First, close any existing long position on Asset1
                self.SellMarket(self.Volume + Math.Abs(self.Position))

                # Then, buy Asset2 using the second portfolio
                if self._asset2Portfolio is not None:
                    asset2Order = Order()
                    asset2Order.Side = Sides.Buy
                    asset2Order.Security = self.Asset2
                    asset2Order.Portfolio = self._asset2Portfolio
                    asset2Order.Volume = self.Volume * self.Beta
                    self.RegisterOrder(asset2Order)

            elif Math.Abs(zScore) < 0.5:
                # Close positions when spread reverts to mean
                if self.Position != 0:
                    if self.Position > 0:
                        self.SellMarket(self.Position)
                    else:
                        self.BuyMarket(Math.Abs(self.Position))

                    # Close position on Asset2
                    if self._asset2Portfolio is not None:
                        asset2Order = Order()
                        asset2Order.Side = Sides.Buy if self.Position > 0 else Sides.Sell
                        asset2Order.Security = self.Asset2
                        asset2Order.Portfolio = self._asset2Portfolio
                        asset2Order.Volume = self.Volume * self.Beta
                        self.RegisterOrder(asset2Order)

        # Reset prices for next update
        self._asset1Price = 0
        self._asset2Price = 0

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return cointegration_pairs_strategy()
