import clr

clr.AddReference("System.Drawing")
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from System.Drawing import Color
from StockSharp.Messages import UnitTypes
from StockSharp.Messages import Unit
from StockSharp.Messages import DataType
from StockSharp.Messages import ICandleMessage
from StockSharp.Messages import CandleStates
from StockSharp.Messages import Sides
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy

class cci_divergence_strategy(Strategy):
    """
    CCI Divergence strategy that looks for divergences between price and CCI
    as potential reversal signals.
    
    See more examples: https://github.com/StockSharp/AlgoTrading
    """
    def __init__(self):
        super(cci_divergence_strategy, self).__init__()
        
        # Initialize internal state
        self._previousPrice = None
        self._previousCci = None
        self._currentPrice = None
        self._currentCci = None
        self._barsSinceDivergence = 0
        self._bullishDivergence = False
        self._bearishDivergence = False

        # Initialize strategy parameters
        self._cciPeriod = self.Param("CciPeriod", 20) \
            .SetDisplay("CCI Period", "Period for CCI calculation", "Indicator Parameters")

        self._divergencePeriod = self.Param("DivergencePeriod", 5) \
            .SetDisplay("Divergence Period", "Number of bars to look back for divergence", "Signal Parameters")

        self._candleType = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._stopLossPercent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Percentage-based stop loss from entry", "Risk Management")

        self._overboughtLevel = self.Param("OverboughtLevel", 100) \
            .SetDisplay("Overbought Level", "CCI level considered overbought", "Signal Parameters")

        self._oversoldLevel = self.Param("OversoldLevel", -100) \
            .SetDisplay("Oversold Level", "CCI level considered oversold", "Signal Parameters")

    @property
    def CciPeriod(self):
        return self._cciPeriod.Value

    @CciPeriod.setter
    def CciPeriod(self, value):
        self._cciPeriod.Value = value

    @property
    def DivergencePeriod(self):
        return self._divergencePeriod.Value

    @DivergencePeriod.setter
    def DivergencePeriod(self, value):
        self._divergencePeriod.Value = value

    @property
    def CandleType(self):
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    @property
    def StopLossPercent(self):
        return self._stopLossPercent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stopLossPercent.Value = value

    @property
    def OverboughtLevel(self):
        return self._overboughtLevel.Value

    @OverboughtLevel.setter
    def OverboughtLevel(self, value):
        self._overboughtLevel.Value = value

    @property
    def OversoldLevel(self):
        return self._oversoldLevel.Value

    @OversoldLevel.setter
    def OversoldLevel(self, value):
        self._oversoldLevel.Value = value

    def OnReseted(self):
        """
        Resets internal state when strategy is reset.
        """
        super(cci_divergence_strategy, self).OnReseted()
        self._previousPrice = None
        self._previousCci = None
        self._currentPrice = None
        self._currentCci = None
        self._barsSinceDivergence = 0
        self._bullishDivergence = False
        self._bearishDivergence = False

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(cci_divergence_strategy, self).OnStarted(time)

        # Reset variables
        self._previousPrice = None
        self._previousCci = None
        self._currentPrice = None
        self._currentCci = None
        self._barsSinceDivergence = 0
        self._bullishDivergence = False
        self._bearishDivergence = False

        # Create CCI indicator
        cci = CommodityChannelIndex()
        cci.Length = self.CciPeriod

        # Create candle subscription
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind CCI to candles
        subscription.Bind(cci, self.ProcessCandle).Start()

        # Enable position protection
        self.StartProtection(
            Unit(0, UnitTypes.Absolute),  # No take profit (managed by exit signals)
            Unit(self.StopLossPercent, UnitTypes.Percent),  # Stop loss at defined percentage
            False  # No trailing stop
        )

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, cci)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, cciValue):
        """
        Process candle and execute trading logic
        
        :param candle: The candle message.
        :param cciValue: The CCI value.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Store previous values before updating
        if self._currentPrice is not None and self._currentCci is not None:
            self._previousPrice = self._currentPrice
            self._previousCci = self._currentCci

        # Update current values
        self._currentPrice = candle.ClosePrice
        self._currentCci = cciValue

        self.LogInfo("Candle: {0}, Close: {1}, CCI: {2:F2}".format(
            candle.OpenTime, candle.ClosePrice, cciValue))

        # Look for divergences once we have enough data
        if (self._previousPrice is not None and self._previousCci is not None and 
            self._currentPrice is not None and self._currentCci is not None):
            self.CheckForDivergences()

        # Process signals based on detected divergences
        self.ProcessDivergenceSignals(candle, cciValue)

    def CheckForDivergences(self):
        """
        Check for bullish and bearish divergences
        """
        # Check for bullish divergence (lower price lows but higher CCI lows)
        if self._currentPrice < self._previousPrice and self._currentCci > self._previousCci:
            self._bullishDivergence = True
            self._bearishDivergence = False
            self._barsSinceDivergence = 0
            self.LogInfo("Bullish Divergence Detected: Price {0}->{1}, CCI {2}->{3}".format(
                self._previousPrice, self._currentPrice, self._previousCci, self._currentCci))
        # Check for bearish divergence (higher price highs but lower CCI highs)
        elif self._currentPrice > self._previousPrice and self._currentCci < self._previousCci:
            self._bearishDivergence = True
            self._bullishDivergence = False
            self._barsSinceDivergence = 0
            self.LogInfo("Bearish Divergence Detected: Price {0}->{1}, CCI {2}->{3}".format(
                self._previousPrice, self._currentPrice, self._previousCci, self._currentCci))
        else:
            self._barsSinceDivergence += 1
            
            # Reset divergence signals after a certain number of bars
            if self._barsSinceDivergence > self.DivergencePeriod:
                self._bullishDivergence = False
                self._bearishDivergence = False

    def ProcessDivergenceSignals(self, candle, cciValue):
        """
        Process trading signals based on divergences
        
        :param candle: The candle message.
        :param cciValue: The CCI value.
        """
        # Entry signals based on detected divergences
        if self._bullishDivergence and self.Position <= 0 and cciValue < self.OversoldLevel:
            # Bullish divergence with CCI in oversold territory - Buy signal
            if self.Position < 0:
                # Close any existing short position
                self.BuyMarket(Math.Abs(self.Position))
                self.LogInfo("Closed short position on bullish divergence")

            # Open new long position
            self.BuyMarket(self.Volume)
            self.LogInfo("Buy signal: Bullish CCI divergence with oversold CCI value: {0:F2}".format(cciValue))
            
            # Reset divergence detection
            self._bullishDivergence = False
        elif self._bearishDivergence and self.Position >= 0 and cciValue > self.OverboughtLevel:
            # Bearish divergence with CCI in overbought territory - Sell signal
            if self.Position > 0:
                # Close any existing long position
                self.SellMarket(self.Position)
                self.LogInfo("Closed long position on bearish divergence")

            # Open new short position
            self.SellMarket(self.Volume)
            self.LogInfo("Sell signal: Bearish CCI divergence with overbought CCI value: {0:F2}".format(cciValue))
            
            # Reset divergence detection
            self._bearishDivergence = False
        
        # Exit signals based on CCI crossing zero line
        elif (self.Position > 0 and self._previousCci is not None and 
              self._previousCci < 0 and cciValue > 0):
            # Exit long position when CCI crosses above zero
            self.SellMarket(self.Position)
            self.LogInfo("Exit long: CCI crossed above zero from negative to positive")
        elif (self.Position < 0 and self._previousCci is not None and 
              self._previousCci > 0 and cciValue < 0):
            # Exit short position when CCI crosses below zero
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo("Exit short: CCI crossed below zero from positive to negative")

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return cci_divergence_strategy()