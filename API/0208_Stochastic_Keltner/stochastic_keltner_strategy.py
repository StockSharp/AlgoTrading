import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import StochasticOscillator, KeltnerChannels, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class stochastic_keltner_strategy(Strategy):
    """
    Strategy based on Stochastic Oscillator and Keltner Channels indicators (#208)
    """

    def __init__(self):
        super(stochastic_keltner_strategy, self).__init__()

        # Initialize strategy parameters
        self._stochPeriod = self.Param("StochPeriod", 14) \
            .SetRange(5, 30) \
            .SetDisplay("Stoch Period", "Period for Stochastic Oscillator", "Stochastic") \
            .SetCanOptimize(True)

        self._stochK = self.Param("StochK", 3) \
            .SetRange(1, 10) \
            .SetDisplay("Stoch %K", "Stochastic %K smoothing period", "Stochastic") \
            .SetCanOptimize(True)

        self._stochD = self.Param("StochD", 3) \
            .SetRange(1, 10) \
            .SetDisplay("Stoch %D", "Stochastic %D smoothing period", "Stochastic") \
            .SetCanOptimize(True)

        self._emaPeriod = self.Param("EmaPeriod", 20) \
            .SetRange(10, 50) \
            .SetDisplay("EMA Period", "EMA period for Keltner Channel", "Keltner") \
            .SetCanOptimize(True)

        self._keltnerMultiplier = self.Param("KeltnerMultiplier", 2.0) \
            .SetRange(1.0, 4.0) \
            .SetDisplay("K Multiplier", "Multiplier for Keltner Channel", "Keltner") \
            .SetCanOptimize(True)

        self._atrPeriod = self.Param("AtrPeriod", 14) \
            .SetRange(7, 28) \
            .SetDisplay("ATR Period", "ATR period for Keltner Channel and stop-loss", "Risk Management") \
            .SetCanOptimize(True)

        self._atrMultiplier = self.Param("AtrMultiplier", 2.0) \
            .SetRange(1.0, 4.0) \
            .SetDisplay("ATR Multiplier", "Multiplier for ATR-based stop-loss", "Risk Management") \
            .SetCanOptimize(True)

        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def StochPeriod(self):
        """Stochastic period"""
        return self._stochPeriod.Value

    @StochPeriod.setter
    def StochPeriod(self, value):
        self._stochPeriod.Value = value

    @property
    def StochK(self):
        """Stochastic %K smoothing period"""
        return self._stochK.Value

    @StochK.setter
    def StochK(self, value):
        self._stochK.Value = value

    @property
    def StochD(self):
        """Stochastic %D smoothing period"""
        return self._stochD.Value

    @StochD.setter
    def StochD(self, value):
        self._stochD.Value = value

    @property
    def EmaPeriod(self):
        """EMA period for Keltner Channel"""
        return self._emaPeriod.Value

    @EmaPeriod.setter
    def EmaPeriod(self, value):
        self._emaPeriod.Value = value

    @property
    def KeltnerMultiplier(self):
        """Keltner Channel multiplier (k)"""
        return self._keltnerMultiplier.Value

    @KeltnerMultiplier.setter
    def KeltnerMultiplier(self, value):
        self._keltnerMultiplier.Value = value

    @property
    def AtrPeriod(self):
        """ATR period for Keltner Channel and stop-loss"""
        return self._atrPeriod.Value

    @AtrPeriod.setter
    def AtrPeriod(self, value):
        self._atrPeriod.Value = value

    @property
    def AtrMultiplier(self):
        """ATR multiplier for stop-loss"""
        return self._atrMultiplier.Value

    @AtrMultiplier.setter
    def AtrMultiplier(self, value):
        self._atrMultiplier.Value = value

    @property
    def CandleType(self):
        """Candle type for strategy"""
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    def OnStarted(self, time):
        """Called when the strategy starts."""
        super(stochastic_keltner_strategy, self).OnStarted(time)

        # Initialize indicators
        stochastic = StochasticOscillator()
        stochastic.K.Length = self.StochPeriod
        stochastic.D.Length = self.StochD

        keltner = KeltnerChannels()
        keltner.Length = self.EmaPeriod

        atr = AverageTrueRange()
        atr.Length = self.AtrPeriod

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(keltner, stochastic, atr, self.ProcessIndicators).Start()

        # Enable ATR-based stop protection
        self.StartProtection(
            takeProfit=Unit(0),
            stopLoss=Unit(self.AtrMultiplier, UnitTypes.Absolute)
        )
        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, keltner)
            self.DrawIndicator(area, stochastic)
            self.DrawOwnTrades(area)

    def ProcessIndicators(self, candle, keltnerValue, stochValue, atrValue):
        """Process indicators for each finished candle."""
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        price = float(candle.ClosePrice)

        upperBand = float(keltnerValue.Upper)
        lowerBand = float(keltnerValue.Lower)
        middleBand = float(keltnerValue.Middle)

        stochK = float(stochValue.K)

        # Trading logic:
        # Long: Stoch %K < 20 && Price < Keltner lower band (oversold at lower band)
        # Short: Stoch %K > 80 && Price > Keltner upper band (overbought at upper band)

        if stochK < 20 and price < lowerBand and self.Position <= 0:
            # Buy signal - Stochastic oversold at Keltner lower band
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)
        elif stochK > 80 and price > upperBand and self.Position >= 0:
            # Sell signal - Stochastic overbought at Keltner upper band
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)
        # Exit conditions
        elif self.Position > 0 and price > middleBand:
            # Exit long position when price returns to middle band
            self.SellMarket(self.Position)
        elif self.Position < 0 and price < middleBand:
            # Exit short position when price returns to middle band
            self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return stochastic_keltner_strategy()
