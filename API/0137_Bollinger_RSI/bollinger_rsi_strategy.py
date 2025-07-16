import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import DataType, UnitTypes, Unit, ICandleMessage, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

class bollinger_rsi_strategy(Strategy):
    """
    Combined strategy that uses Bollinger Bands and RSI indicators
    for mean reversion trading.
    """
    # Strategy constructor.
    def __init__(self):
        super(bollinger_rsi_strategy, self).__init__()

        # Initialize strategy parameters
        self._bollingerPeriod = self.Param("BollingerPeriod", 20) \
            .SetDisplay("Bollinger Period", "Period of the Bollinger Bands indicator", "Indicators") \
            .SetGreaterThanZero() \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5)

        self._bollingerDeviation = self.Param("BollingerDeviation", 2.0) \
            .SetDisplay("Bollinger Deviation", "Standard deviation multiplier for Bollinger Bands", "Indicators") \
            .SetGreaterThanZero() \
            .SetCanOptimize(True) \
            .SetOptimize(1.5, 3.0, 0.5)

        self._rsiPeriod = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "Period of the RSI indicator", "Indicators") \
            .SetGreaterThanZero() \
            .SetCanOptimize(True) \
            .SetOptimize(7, 21, 7)

        self._rsiOversold = self.Param("RsiOversold", 30) \
            .SetDisplay("RSI Oversold", "RSI level considered oversold", "Indicators") \
            .SetNotNegative() \
            .SetCanOptimize(True) \
            .SetOptimize(20, 40, 5)

        self._rsiOverbought = self.Param("RsiOverbought", 70) \
            .SetDisplay("RSI Overbought", "RSI level considered overbought", "Indicators") \
            .SetNotNegative() \
            .SetCanOptimize(True) \
            .SetOptimize(60, 80, 5)

        self._candleType = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._stopLossAtr = self.Param("StopLossAtr", 2.0) \
            .SetDisplay("Stop Loss ATR", "Stop loss as ATR multiplier", "Risk Management") \
            .SetGreaterThanZero() \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

    @property
    def BollingerPeriod(self):
        """Bollinger Bands period."""
        return self._bollingerPeriod.Value

    @BollingerPeriod.setter
    def BollingerPeriod(self, value):
        self._bollingerPeriod.Value = value

    @property
    def BollingerDeviation(self):
        """Bollinger Bands standard deviation multiplier."""
        return self._bollingerDeviation.Value

    @BollingerDeviation.setter
    def BollingerDeviation(self, value):
        self._bollingerDeviation.Value = value

    @property
    def RsiPeriod(self):
        """RSI period."""
        return self._rsiPeriod.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsiPeriod.Value = value

    @property
    def RsiOversold(self):
        """RSI oversold level."""
        return self._rsiOversold.Value

    @RsiOversold.setter
    def RsiOversold(self, value):
        self._rsiOversold.Value = value

    @property
    def RsiOverbought(self):
        """RSI overbought level."""
        return self._rsiOverbought.Value

    @RsiOverbought.setter
    def RsiOverbought(self, value):
        self._rsiOverbought.Value = value

    @property
    def CandleType(self):
        """Candle type for strategy calculation."""
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    @property
    def StopLossAtr(self):
        """Stop-loss in ATR multiplier."""
        return self._stopLossAtr.Value

    @StopLossAtr.setter
    def StopLossAtr(self, value):
        self._stopLossAtr.Value = value

    def GetWorkingSecurities(self):
        """!! REQUIRED!! Return securities and candle types used."""
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        """Called when the strategy starts."""
        super(bollinger_rsi_strategy, self).OnStarted(time)

        # Create indicators
        bollinger = BollingerBands()
        bollinger.Length = self.BollingerPeriod
        bollinger.Width = self.BollingerDeviation

        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(bollinger, rsi, self.ProcessCandles).Start()

        # Setup position protection
        self.StartProtection(
            Unit(0, UnitTypes.Absolute),  # No take profit
            Unit(self.StopLossAtr, UnitTypes.Absolute)  # Stop loss as ATR multiplier
        )

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def ProcessCandles(self, candle, bollingerValue, rsiValue):
        """Process candles and indicator values."""
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        bollingerTyped = bollingerValue
        upperBand = bollingerTyped.UpBand
        lowerBand = bollingerTyped.LowBand
        middleBand = bollingerTyped.MovingAverage
        rsiTyped = to_float(rsiValue)

        # Long entry: price below lower Bollinger Band and RSI oversold
        if candle.ClosePrice < lowerBand and rsiTyped < self.RsiOversold and self.Position <= 0:
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)
        # Short entry: price above upper Bollinger Band and RSI overbought
        elif candle.ClosePrice > upperBand and rsiTyped > self.RsiOverbought and self.Position >= 0:
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)
        # Long exit: price returns to middle band
        elif self.Position > 0 and candle.ClosePrice > middleBand:
            self.SellMarket(Math.Abs(self.Position))
        # Short exit: price returns to middle band
        elif self.Position < 0 and candle.ClosePrice < middleBand:
            self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return bollinger_rsi_strategy()
