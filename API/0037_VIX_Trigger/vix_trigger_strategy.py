import clr

clr.AddReference("System.Drawing")
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from System.Drawing import Color
from StockSharp.Messages import UnitTypes, Unit, DataType, ICandleMessage, CandleStates, Sides
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Security, Subscription
from datatype_extensions import *

class vix_trigger_strategy(Strategy):
    """
    Strategy that trades based on VIX (Volatility Index) movements.
    It enters positions when VIX is rising (indicating increasing fear/volatility in the market)
    and price is moving in an expected direction relative to its moving average.
    
    """
    def __init__(self):
        super(vix_trigger_strategy, self).__init__()
        
        # Initialize internal state
        self._prevVix = 0
        self._latestVix = 0
        self._isVixRising = False

        # Initialize strategy parameters
        self._maPeriod = self.Param("MAPeriod", 20) \
            .SetDisplay("MA Period", "Period for Moving Average calculation", "Technical Parameters")

        self._stopLossPercent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss as percentage from entry price", "Risk Management")

        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "Data")

        self._vixSecurity = self.Param[Security]("VixSecurity", None) \
            .SetDisplay("VIX Security", "VIX Security to use for signals", "Data")

    @property
    def MAPeriod(self):
        return self._maPeriod.Value

    @MAPeriod.setter
    def MAPeriod(self, value):
        self._maPeriod.Value = value

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

    @property
    def VixSecurity(self):
        return self._vixSecurity.Value

    @VixSecurity.setter
    def VixSecurity(self, value):
        self._vixSecurity.Value = value

    def OnReseted(self):
        """
        Resets internal state when strategy is reset.
        """
        super(vix_trigger_strategy, self).OnReseted()
        self._prevVix = 0
        self._latestVix = 0
        self._isVixRising = False

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(vix_trigger_strategy, self).OnStarted(time)

        # Reset state variables
        self._prevVix = 0
        self._latestVix = 0
        self._isVixRising = False

        # Create indicator
        sma = SimpleMovingAverage()
        sma.Length = self.MAPeriod

        # Create subscriptions
        mainSubscription = self.SubscribeCandles(self.CandleType)
        vixSubscription = self.SubscribeCandles(self.CandleType, self.VixSecurity)

        # Bind indicator to main security candles
        mainSubscription.Bind(sma, self.ProcessMainCandle).Start()

        # Process VIX candles separately
        vixSubscription.Bind(self.ProcessVixCandle).Start()

        # Configure chart if GUI is available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, mainSubscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

        # Setup protection with stop-loss
        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent)
        )
    def ProcessVixCandle(self, candle):
        """
        Process VIX candle to track VIX movements
        
        :param candle: The VIX candle message.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Store latest VIX value
        self._latestVix = candle.ClosePrice

        # Initialize _prevVix on first VIX candle
        if self._prevVix == 0:
            self._prevVix = self._latestVix
            return

        # Check if VIX is rising
        self._isVixRising = self._latestVix > self._prevVix

        # Update previous VIX value
        self._prevVix = self._latestVix

    def ProcessMainCandle(self, candle, smaValue):
        """
        Process main security candle and check for trading signals
        
        :param candle: The main security candle message.
        :param smaValue: The current SMA value.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Check if we have received VIX data
        if self._prevVix == 0:
            return

        # Determine price position relative to MA
        isPriceBelowMA = candle.ClosePrice < smaValue

        if self.Position == 0:
            # No position - check for entry signals
            if self._isVixRising and isPriceBelowMA:
                # VIX is rising and price is below MA - buy (contrarian strategy)
                self.BuyMarket(self.Volume)
            elif self._isVixRising and not isPriceBelowMA:
                # VIX is rising and price is above MA - sell (contrarian strategy)
                self.SellMarket(self.Volume)
        elif self.Position > 0:
            # Long position - check for exit signal
            if not self._isVixRising:
                # VIX is decreasing - exit long
                self.SellMarket(self.Position)
        elif self.Position < 0:
            # Short position - check for exit signal
            if not self._isVixRising:
                # VIX is decreasing - exit short
                self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return vix_trigger_strategy()