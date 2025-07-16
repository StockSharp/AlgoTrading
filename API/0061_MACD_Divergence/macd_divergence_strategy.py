import clr

clr.AddReference("System.Drawing")
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from System.Drawing import Color
from StockSharp.Messages import UnitTypes, Unit, DataType, ICandleMessage, CandleStates, Sides
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class macd_divergence_strategy(Strategy):
    """
    MACD Divergence strategy that looks for divergences between price and MACD
    as potential reversal signals.
    
    """
    def __init__(self):
        super(macd_divergence_strategy, self).__init__()
        
        # Initialize internal state
        self._previousPrice = None
        self._previousMacd = None
        self._currentPrice = None
        self._currentMacd = None
        self._barsSinceDivergence = 0
        self._bullishDivergence = False
        self._bearishDivergence = False

        # Initialize strategy parameters
        self._fastMacdPeriod = self.Param("FastMacdPeriod", 12) \
            .SetDisplay("Fast MACD Period", "Fast EMA period for MACD", "Indicator Parameters")

        self._slowMacdPeriod = self.Param("SlowMacdPeriod", 26) \
            .SetDisplay("Slow MACD Period", "Slow EMA period for MACD", "Indicator Parameters")

        self._signalPeriod = self.Param("SignalPeriod", 9) \
            .SetDisplay("Signal Period", "Signal line period for MACD", "Indicator Parameters")

        self._divergencePeriod = self.Param("DivergencePeriod", 5) \
            .SetDisplay("Divergence Period", "Number of bars to look back for divergence", "Signal Parameters")

        self._candleType = self.Param("CandleType", tf(15)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._stopLossPercent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Percentage-based stop loss from entry", "Risk Management")

    @property
    def FastMacdPeriod(self):
        return self._fastMacdPeriod.Value

    @FastMacdPeriod.setter
    def FastMacdPeriod(self, value):
        self._fastMacdPeriod.Value = value

    @property
    def SlowMacdPeriod(self):
        return self._slowMacdPeriod.Value

    @SlowMacdPeriod.setter
    def SlowMacdPeriod(self, value):
        self._slowMacdPeriod.Value = value

    @property
    def SignalPeriod(self):
        return self._signalPeriod.Value

    @SignalPeriod.setter
    def SignalPeriod(self, value):
        self._signalPeriod.Value = value

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

    def OnReseted(self):
        """
        Resets internal state when strategy is reset.
        """
        super(macd_divergence_strategy, self).OnReseted()
        self._previousPrice = None
        self._previousMacd = None
        self._currentPrice = None
        self._currentMacd = None
        self._barsSinceDivergence = 0
        self._bullishDivergence = False
        self._bearishDivergence = False

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(macd_divergence_strategy, self).OnStarted(time)

        # Reset variables
        self._previousPrice = None
        self._previousMacd = None
        self._currentPrice = None
        self._currentMacd = None
        self._barsSinceDivergence = 0
        self._bullishDivergence = False
        self._bearishDivergence = False

        # Create MACD indicator
        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self.FastMacdPeriod
        macd.Macd.LongMa.Length = self.SlowMacdPeriod
        macd.SignalMa.Length = self.SignalPeriod

        # Create candle subscription
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind MACD to candles
        subscription.BindEx(macd, self.ProcessCandle).Start()

        # Enable position protection
        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent),
            isStopTrailing=False
        )
        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, macd)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, macdValue):
        """
        Process candle and execute trading logic
        
        :param candle: The candle message.
        :param macdValue: The MACD indicator value.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        try:
            # Extract MACD values - be careful with the order of indexes
            macdTyped = macdValue
            
            if macdTyped.Macd is None or macdTyped.Signal is None:
                return
            
            macd = macdTyped.Macd
            signal = macdTyped.Signal

            # Store previous values before updating
            if self._currentPrice is not None and self._currentMacd is not None:
                self._previousPrice = self._currentPrice
                self._previousMacd = self._currentMacd

            # Update current values
            self._currentPrice = candle.ClosePrice
            self._currentMacd = macd

            self.LogInfo("Candle: {0}, Close: {1}, MACD: {2:F4}, Signal: {3:F4}".format(
                candle.OpenTime, candle.ClosePrice, macd, signal))

            # Look for divergences once we have enough data
            if (self._previousPrice is not None and self._previousMacd is not None and 
                self._currentPrice is not None and self._currentMacd is not None):
                self.CheckForDivergences()

            # Process signals based on detected divergences
            self.ProcessDivergenceSignals(candle, macd, signal)
        except Exception as ex:
            self.LogError("Error processing MACD values: {0}".format(str(ex)))

    def CheckForDivergences(self):
        """
        Check for bullish and bearish divergences
        """
        # Check for bullish divergence (lower price lows but higher MACD lows)
        if self._currentPrice < self._previousPrice and self._currentMacd > self._previousMacd:
            self._bullishDivergence = True
            self._bearishDivergence = False
            self._barsSinceDivergence = 0
            self.LogInfo("Bullish Divergence Detected: Price {0}->{1}, MACD {2}->{3}".format(
                self._previousPrice, self._currentPrice, self._previousMacd, self._currentMacd))
        # Check for bearish divergence (higher price highs but lower MACD highs)
        elif self._currentPrice > self._previousPrice and self._currentMacd < self._previousMacd:
            self._bearishDivergence = True
            self._bullishDivergence = False
            self._barsSinceDivergence = 0
            self.LogInfo("Bearish Divergence Detected: Price {0}->{1}, MACD {2}->{3}".format(
                self._previousPrice, self._currentPrice, self._previousMacd, self._currentMacd))
        else:
            self._barsSinceDivergence += 1
            
            # Reset divergence signals after a certain number of bars
            if self._barsSinceDivergence > self.DivergencePeriod:
                self._bullishDivergence = False
                self._bearishDivergence = False

    def ProcessDivergenceSignals(self, candle, macdLine, signalLine):
        """
        Process trading signals based on divergences
        
        :param candle: The candle message.
        :param macdLine: The MACD line value.
        :param signalLine: The signal line value.
        """
        # Entry signals based on detected divergences
        if self._bullishDivergence and self.Position <= 0 and macdLine > signalLine:
            # Bullish divergence with MACD crossing above signal - Buy signal
            if self.Position < 0:
                # Close any existing short position
                self.BuyMarket(Math.Abs(self.Position))
                self.LogInfo("Closed short position on bullish divergence")

            # Open new long position
            self.BuyMarket(self.Volume)
            self.LogInfo("Buy signal: Bullish MACD divergence with signal line cross")
            
            # Reset divergence detection
            self._bullishDivergence = False
        elif self._bearishDivergence and self.Position >= 0 and macdLine < signalLine:
            # Bearish divergence with MACD crossing below signal - Sell signal
            if self.Position > 0:
                # Close any existing long position
                self.SellMarket(self.Position)
                self.LogInfo("Closed long position on bearish divergence")

            # Open new short position
            self.SellMarket(self.Volume)
            self.LogInfo("Sell signal: Bearish MACD divergence with signal line cross")
            
            # Reset divergence detection
            self._bearishDivergence = False
        
        # Exit signals based on MACD crossing the signal line
        elif self.Position > 0 and macdLine < signalLine:
            # Exit long position when MACD crosses below signal
            self.SellMarket(self.Position)
            self.LogInfo("Exit long: MACD crossed below signal line")
        elif self.Position < 0 and macdLine > signalLine:
            # Exit short position when MACD crosses above signal
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo("Exit short: MACD crossed above signal line")

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return macd_divergence_strategy()