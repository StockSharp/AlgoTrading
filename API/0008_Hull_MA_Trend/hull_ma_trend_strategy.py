import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import DataType, UnitTypes, Unit
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import HullMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class hull_ma_trend_strategy(Strategy):
    """
    Strategy based on Hull Moving Average trend.
    It enters long position when Hull MA is rising 
    and short position when Hull MA is falling.
    
    """
    
    def __init__(self):
        super(hull_ma_trend_strategy, self).__init__()
        
        # Initialize strategy parameters
        self._hma_period = self.Param("HmaPeriod", 9) \
            .SetDisplay("HMA Period", "Period for Hull Moving Average", "Indicators")
        
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "Period for Average True Range (stop-loss)", "Risk parameters")
        
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0) \
            .SetDisplay("ATR Multiplier", "Multiplier for ATR to determine stop-loss distance", "Risk parameters")
        
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        
        # Current state
        self._prev_hma_value = 0.0

    @property
    def hma_period(self):
        """Period for Hull Moving Average."""
        return self._hma_period.Value

    @hma_period.setter
    def hma_period(self, value):
        self._hma_period.Value = value

    @property
    def atr_period(self):
        """Period for ATR calculation (stop-loss)."""
        return self._atr_period.Value

    @atr_period.setter
    def atr_period(self, value):
        self._atr_period.Value = value

    @property
    def atr_multiplier(self):
        """Multiplier for ATR to determine stop-loss distance."""
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
        super(hull_ma_trend_strategy, self).OnReseted()
        self._prev_hma_value = 0.0

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(hull_ma_trend_strategy, self).OnStarted(time)

        # Initialize state
        self._prev_hma_value = 0.0

        # Create indicators
        hma = HullMovingAverage()
        hma.Length = self.hma_period
        
        atr = AverageTrueRange()
        atr.Length = self.atr_period

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(hma, atr, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, hma)
            self.DrawOwnTrades(area)

        # Setup protection using ATR
        self.StartProtection(
            takeProfit=None,
            stopLoss=Unit(self.atr_multiplier, UnitTypes.Absolute)
        )
    def ProcessCandle(self, candle, hma_value, atr_value):
        """
        Processes each finished candle and executes Hull MA trend-based trading logic.
        
        :param candle: The processed candle message.
        :param hma_value: The current value of the Hull MA.
        :param atr_value: The current value of the ATR indicator.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Convert HMA value to float
        hma_decimal = float(hma_value)

        # Skip the first received value for proper trend determination
        if self._prev_hma_value == 0:
            self._prev_hma_value = hma_decimal
            return

        # Determine HMA direction
        is_hma_rising = hma_decimal > self._prev_hma_value
        is_hma_falling = hma_decimal < self._prev_hma_value

        # Trading logic
        if is_hma_rising and self.Position <= 0:
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)
            self.LogInfo("Buy signal: HMA rising from {0} to {1}".format(
                self._prev_hma_value, hma_decimal))
        elif is_hma_falling and self.Position >= 0:
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)
            self.LogInfo("Sell signal: HMA falling from {0} to {1}".format(
                self._prev_hma_value, hma_decimal))
        # Exit logic for direction change
        elif not is_hma_rising and self.Position > 0:
            self.SellMarket(self.Position)
            self.LogInfo("Exit long: HMA started falling from {0} to {1}".format(
                self._prev_hma_value, hma_decimal))
        elif not is_hma_falling and self.Position < 0:
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo("Exit short: HMA started rising from {0} to {1}".format(
                self._prev_hma_value, hma_decimal))

        # Update previous HMA value
        self._prev_hma_value = hma_decimal

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return hull_ma_trend_strategy()