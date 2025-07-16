import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import DataType, UnitTypes, Unit
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import RateOfChange, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class roc_impulse_strategy(Strategy):
    """
    Strategy based on Rate of Change (ROC) impulse.
    It enters long when ROC is positive and increasing (positive momentum),
    and short when ROC is negative and decreasing (negative momentum).
    
    """
    
    def __init__(self):
        super(roc_impulse_strategy, self).__init__()
        
        # Initialize strategy parameters
        self._roc_period = self.Param("RocPeriod", 12) \
            .SetDisplay("ROC Period", "Period for Rate of Change calculation", "Indicators")
        
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0) \
            .SetDisplay("ATR Multiplier", "Multiplier for ATR stop loss", "Risk Management")
        
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        
        # State tracking
        self._previous_roc = 0.0
        self._is_first_candle = True

    @property
    def roc_period(self):
        """ROC period."""
        return self._roc_period.Value

    @roc_period.setter
    def roc_period(self, value):
        self._roc_period.Value = value

    @property
    def atr_multiplier(self):
        """ATR multiplier for stop-loss."""
        return self._atr_multiplier.Value

    @atr_multiplier.setter
    def atr_multiplier(self, value):
        self._atr_multiplier.Value = value

    @property
    def candle_type(self):
        """Candle type."""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        """
        Resets internal state when strategy is reset.
        """
        super(roc_impulse_strategy, self).OnReseted()
        self._previous_roc = 0.0
        self._is_first_candle = True

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(roc_impulse_strategy, self).OnStarted(time)

        # Reset state variables
        self._previous_roc = 0.0
        self._is_first_candle = True

        # Create indicators
        roc = RateOfChange()
        roc.Length = self.roc_period
        
        atr = AverageTrueRange()
        atr.Length = 14

        # Subscribe to candles and bind indicators
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(roc, atr, self.ProcessCandle).Start()

        # Enable position protection with ATR-based stop loss
        self.StartProtection(
            takeProfit=None,
            stopLoss=Unit(self.atr_multiplier, UnitTypes.Absolute)
        )
        # Setup chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, roc)
            self.DrawIndicator(area, atr)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, roc_value, atr_value):
        """
        Processes each finished candle and executes ROC impulse logic.
        
        :param candle: The processed candle message.
        :param roc_value: The current value of the ROC indicator.
        :param atr_value: The current value of the ATR indicator.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Convert values to float
        roc_decimal = float(roc_value)

        if self._is_first_candle:
            self._previous_roc = roc_decimal
            self._is_first_candle = False
            return

        # Entry logic for long positions:
        # ROC is positive and increasing (positive momentum)
        if roc_decimal > 0 and roc_decimal > self._previous_roc and self.Position <= 0:
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)
            self.LogInfo("Buy signal: ROC positive and increasing. Current: {0:F4}, Previous: {1:F4}".format(
                roc_decimal, self._previous_roc))
        
        # Entry logic for short positions:
        # ROC is negative and decreasing (negative momentum)
        elif roc_decimal < 0 and roc_decimal < self._previous_roc and self.Position >= 0:
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)
            self.LogInfo("Sell signal: ROC negative and decreasing. Current: {0:F4}, Previous: {1:F4}".format(
                roc_decimal, self._previous_roc))

        # Exit logic for long positions: ROC turns negative
        if self.Position > 0 and roc_decimal < 0:
            self.SellMarket(Math.Abs(self.Position))
            self.LogInfo("Exiting long position: ROC turned negative at {0:F4}".format(roc_decimal))
        
        # Exit logic for short positions: ROC turns positive
        elif self.Position < 0 and roc_decimal > 0:
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo("Exiting short position: ROC turned positive at {0:F4}".format(roc_decimal))

        # Store current ROC value for next comparison
        self._previous_roc = roc_decimal

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return roc_impulse_strategy()
