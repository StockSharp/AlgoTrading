import clr

clr.AddReference("System.Drawing")
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from System.Drawing import Color
from StockSharp.Messages import UnitTypes, Unit, DataType, ICandleMessage, CandleStates, Sides
from StockSharp.Algo.Indicators import ParabolicSar, StochasticOscillator
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class parabolic_sar_stochastic_strategy(Strategy):
    """
    Implementation of strategy - Parabolic SAR + Stochastic.
    Buy when price is above SAR and Stochastic %K is below 20 (oversold).
    Sell when price is below SAR and Stochastic %K is above 80 (overbought).

    See more examples: https://github.com/StockSharp/AlgoTrading
    """
    def __init__(self):
        super(parabolic_sar_stochastic_strategy, self).__init__()

        # Initialize strategy parameters
        self._accelerationFactor = self.Param("AccelerationFactor", 0.02) \
            .SetRange(0.01, 0.2) \
            .SetDisplay("Acceleration Factor", "Initial acceleration factor for SAR", "SAR Parameters")

        self._maxAccelerationFactor = self.Param("MaxAccelerationFactor", 0.2) \
            .SetRange(0.05, 0.5) \
            .SetDisplay("Max Acceleration Factor", "Maximum acceleration factor for SAR", "SAR Parameters")

        self._stochK = self.Param("StochK", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Stochastic %K", "Stochastic %K smoothing period", "Stochastic Parameters")

        self._stochD = self.Param("StochD", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Stochastic %D", "Stochastic %D smoothing period", "Stochastic Parameters")

        self._stochPeriod = self.Param("StochPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("Stochastic Period", "Main period for Stochastic oscillator", "Stochastic Parameters")

        self._stochOversold = self.Param("StochOversold", 20.0) \
            .SetRange(1, 100) \
            .SetDisplay("Oversold Level", "Level below which market is considered oversold", "Stochastic Parameters")

        self._stochOverbought = self.Param("StochOverbought", 80.0) \
            .SetRange(1, 100) \
            .SetDisplay("Overbought Level", "Level above which market is considered overbought", "Stochastic Parameters")

        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Candle type for strategy", "General")

        self._lastStochK = 50  # Initialize to a neutral value
        self._isAboveSar = False

    @property
    def AccelerationFactor(self):
        """Parabolic SAR acceleration factor."""
        return self._accelerationFactor.Value

    @AccelerationFactor.setter
    def AccelerationFactor(self, value):
        self._accelerationFactor.Value = value

    @property
    def MaxAccelerationFactor(self):
        """Parabolic SAR maximum acceleration factor."""
        return self._maxAccelerationFactor.Value

    @MaxAccelerationFactor.setter
    def MaxAccelerationFactor(self, value):
        self._maxAccelerationFactor.Value = value

    @property
    def StochK(self):
        """Stochastic %K period."""
        return self._stochK.Value

    @StochK.setter
    def StochK(self, value):
        self._stochK.Value = value

    @property
    def StochD(self):
        """Stochastic %D period."""
        return self._stochD.Value

    @StochD.setter
    def StochD(self, value):
        self._stochD.Value = value

    @property
    def StochPeriod(self):
        """Stochastic main period."""
        return self._stochPeriod.Value

    @StochPeriod.setter
    def StochPeriod(self, value):
        self._stochPeriod.Value = value

    @property
    def StochOversold(self):
        """Stochastic oversold level."""
        return self._stochOversold.Value

    @StochOversold.setter
    def StochOversold(self, value):
        self._stochOversold.Value = value

    @property
    def StochOverbought(self):
        """Stochastic overbought level."""
        return self._stochOverbought.Value

    @StochOverbought.setter
    def StochOverbought(self, value):
        self._stochOverbought.Value = value

    @property
    def CandleType(self):
        """Candle type used for strategy."""
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    def OnStarted(self, time):
        super(parabolic_sar_stochastic_strategy, self).OnStarted(time)

        # Create indicators
        parabolic_sar = ParabolicSar()
        parabolic_sar.AccelerationStep = self.AccelerationFactor
        parabolic_sar.AccelerationMax = self.MaxAccelerationFactor

        stochastic = StochasticOscillator()
        stochastic.K.Length = self.StochK
        stochastic.D.Length = self.StochD

        # Reset state
        self._lastStochK = 50
        self._isAboveSar = False

        # Setup candle subscription
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind indicators to candles
        subscription.BindEx(parabolic_sar, stochastic, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, parabolic_sar)

            # Create separate area for Stochastic
            stochArea = self.CreateChartArea()
            if stochArea is not None:
                self.DrawIndicator(stochArea, stochastic)

            self.DrawOwnTrades(area)

        # SAR itself will act as a dynamic stop-loss by reversing position
        # when price crosses SAR in the opposite direction

    def ProcessCandle(self, candle: ICandleMessage, sarValue, stochValue):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        stochTyped = stochValue
        if hasattr(stochTyped, 'K') and stochTyped.K is not None:
            stochK = float(stochTyped.K)
        else:
            return

        sarDec = to_float(sarValue)

        currentPrice = candle.ClosePrice
        priceAboveSar = currentPrice > sarDec

        self.LogInfo(
            "Candle: {0}, Close: {1}, Parabolic SAR: {2}, Stochastic %K: {3}, IsAboveSAR: {4}, OldIsAboveSAR: {5}".format(
                candle.OpenTime, currentPrice, sarDec, stochK, priceAboveSar, self._isAboveSar
            )
        )

        # Check for SAR reversal signal (price crossing SAR)
        sarSignalChange = priceAboveSar != self._isAboveSar

        # Trading rules
        if priceAboveSar and stochK < self.StochOversold and self.Position <= 0:
            # Buy signal - price above SAR (uptrend) and Stochastic oversold
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)

            self.LogInfo(
                "Buy signal: Price above SAR and Stochastic oversold ({0} < {1}). Volume: {2}".format(
                    stochK, self.StochOversold, volume
                )
            )
        elif not priceAboveSar and stochK > self.StochOverbought and self.Position >= 0:
            # Sell signal - price below SAR (downtrend) and Stochastic overbought
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)

            self.LogInfo(
                "Sell signal: Price below SAR and Stochastic overbought ({0} > {1}). Volume: {2}".format(
                    stochK, self.StochOverbought, volume
                )
            )
        # Check for SAR reversal - exit signals
        elif sarSignalChange:
            if not priceAboveSar and self.Position > 0:
                # Exit long position when price crosses below SAR
                self.SellMarket(self.Position)
                self.LogInfo("Exit long: Price crossed below SAR. Position: {0}".format(self.Position))
            elif priceAboveSar and self.Position < 0:
                # Exit short position when price crosses above SAR
                self.BuyMarket(Math.Abs(self.Position))
                self.LogInfo("Exit short: Price crossed above SAR. Position: {0}".format(self.Position))

        # Update state for next iteration
        self._lastStochK = stochK
        self._isAboveSar = priceAboveSar

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return parabolic_sar_stochastic_strategy()
