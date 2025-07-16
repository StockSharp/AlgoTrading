import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal, BollingerBands, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class macd_bollinger_strategy(Strategy):
    """
    Strategy based on MACD and Bollinger Bands indicators (#206)
    """

    def __init__(self):
        super(macd_bollinger_strategy, self).__init__()

        # MACD fast EMA period
        self._macdFast = self.Param("MacdFast", 12) \
            .SetRange(5, 20) \
            .SetDisplay("MACD Fast", "MACD fast EMA period", "MACD") \
            .SetCanOptimize(True)

        # MACD slow EMA period
        self._macdSlow = self.Param("MacdSlow", 26) \
            .SetRange(15, 40) \
            .SetDisplay("MACD Slow", "MACD slow EMA period", "MACD") \
            .SetCanOptimize(True)

        # MACD signal line period
        self._macdSignal = self.Param("MacdSignal", 9) \
            .SetRange(5, 15) \
            .SetDisplay("MACD Signal", "MACD signal line period", "MACD") \
            .SetCanOptimize(True)

        # Bollinger Bands period
        self._bollingerPeriod = self.Param("BollingerPeriod", 20) \
            .SetRange(10, 50) \
            .SetDisplay("Bollinger Period", "Bollinger Bands period", "Bollinger") \
            .SetCanOptimize(True)

        # Bollinger Bands standard deviation multiplier
        self._bollingerDeviation = self.Param("BollingerDeviation", 2.0) \
            .SetRange(1.0, 3.0) \
            .SetDisplay("Bollinger Deviation", "Bollinger Bands standard deviation multiplier", "Bollinger") \
            .SetCanOptimize(True)

        # ATR period for stop-loss
        self._atrPeriod = self.Param("AtrPeriod", 14) \
            .SetRange(7, 28) \
            .SetDisplay("ATR Period", "ATR period for stop-loss calculation", "Risk Management") \
            .SetCanOptimize(True)

        # ATR multiplier for stop-loss
        self._atrMultiplier = self.Param("AtrMultiplier", 2.0) \
            .SetRange(1.0, 4.0) \
            .SetDisplay("ATR Multiplier", "Multiplier for ATR-based stop-loss", "Risk Management") \
            .SetCanOptimize(True)

        # Candle type for strategy
        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def MacdFast(self):
        """MACD fast EMA period"""
        return self._macdFast.Value

    @MacdFast.setter
    def MacdFast(self, value):
        self._macdFast.Value = value

    @property
    def MacdSlow(self):
        """MACD slow EMA period"""
        return self._macdSlow.Value

    @MacdSlow.setter
    def MacdSlow(self, value):
        self._macdSlow.Value = value

    @property
    def MacdSignal(self):
        """MACD signal line period"""
        return self._macdSignal.Value

    @MacdSignal.setter
    def MacdSignal(self, value):
        self._macdSignal.Value = value

    @property
    def BollingerPeriod(self):
        """Bollinger Bands period"""
        return self._bollingerPeriod.Value

    @BollingerPeriod.setter
    def BollingerPeriod(self, value):
        self._bollingerPeriod.Value = value

    @property
    def BollingerDeviation(self):
        """Bollinger Bands standard deviation multiplier"""
        return self._bollingerDeviation.Value

    @BollingerDeviation.setter
    def BollingerDeviation(self, value):
        self._bollingerDeviation.Value = value

    @property
    def AtrPeriod(self):
        """ATR period for stop-loss"""
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

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        super(macd_bollinger_strategy, self).OnStarted(time)

        # Initialize indicators
        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.LongMa.Length = self.MacdSlow
        macd.Macd.ShortMa.Length = self.MacdFast
        macd.SignalMa.Length = self.MacdSignal

        bollinger = BollingerBands()
        bollinger.Length = self.BollingerPeriod
        bollinger.Width = self.BollingerDeviation

        atr = AverageTrueRange()
        atr.Length = self.AtrPeriod

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(bollinger, macd, atr, self.ProcessIndicators).Start()

        # Enable ATR-based stop protection
        self.StartProtection(Unit(0), Unit(self.AtrMultiplier, UnitTypes.Absolute))

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)
            self.DrawIndicator(area, macd)
            self.DrawOwnTrades(area)

    def ProcessIndicators(self, candle, bollingerValue, macdValue, atrValue):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        upperBand = bollingerValue.UpBand
        lowerBand = bollingerValue.LowBand
        middleBand = bollingerValue.MovingAverage

        macd_line = macdValue.Macd
        signal_line = macdValue.Signal

        price = candle.ClosePrice

        # Trading logic:
        # Long: MACD > Signal && Price < BB_lower (trend up with oversold conditions)
        # Short: MACD < Signal && Price > BB_upper (trend down with overbought conditions)
        macd_cross_over = macd_line > signal_line

        if macd_cross_over and price < lowerBand and self.Position <= 0:
            # Buy signal - MACD crossing above signal line at lower Bollinger Band
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)
        elif not macd_cross_over and price > upperBand and self.Position >= 0:
            # Sell signal - MACD crossing below signal line at upper Bollinger Band
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
        return macd_bollinger_strategy()
