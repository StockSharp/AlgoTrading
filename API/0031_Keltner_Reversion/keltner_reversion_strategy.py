import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import DataType
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class keltner_reversion_strategy(Strategy):
    """
    Strategy that trades on mean reversion using Keltner Channels.
    It opens positions when price touches or breaks through the upper or lower Keltner Channel bands
    and exits when price reverts to the middle band (EMA).
    
    """
    
    def __init__(self):
        super(keltner_reversion_strategy, self).__init__()
        
        # Initialize strategy parameters
        self._ema_period = self.Param("EmaPeriod", 20) \
            .SetDisplay("EMA Period", "Period for EMA calculation (middle band)", "Technical Parameters")
        
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "Period for ATR calculation", "Technical Parameters")
        
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0) \
            .SetDisplay("ATR Multiplier", "ATR multiplier for Keltner Channel width", "Technical Parameters")
        
        self._stop_loss_atr_multiplier = self.Param("StopLossAtrMultiplier", 2.0) \
            .SetDisplay("ATR Multiplier (Stop Loss)", "ATR multiplier for stop-loss calculation", "Risk Management")
        
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "Technical Parameters")

    @property
    def ema_period(self):
        """Period for EMA calculation (middle band) (default: 20)"""
        return self._ema_period.Value

    @ema_period.setter
    def ema_period(self, value):
        self._ema_period.Value = value

    @property
    def atr_period(self):
        """Period for ATR calculation (default: 14)"""
        return self._atr_period.Value

    @atr_period.setter
    def atr_period(self, value):
        self._atr_period.Value = value

    @property
    def atr_multiplier(self):
        """ATR multiplier for Keltner Channel width (default: 2.0)"""
        return self._atr_multiplier.Value

    @atr_multiplier.setter
    def atr_multiplier(self, value):
        self._atr_multiplier.Value = value

    @property
    def stop_loss_atr_multiplier(self):
        """ATR multiplier for stop-loss calculation (default: 2.0)"""
        return self._stop_loss_atr_multiplier.Value

    @stop_loss_atr_multiplier.setter
    def stop_loss_atr_multiplier(self, value):
        self._stop_loss_atr_multiplier.Value = value

    @property
    def candle_type(self):
        """Type of candles used for strategy calculation"""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        """
        Resets internal state when strategy is reset.
        """
        super(keltner_reversion_strategy, self).OnReseted()

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(keltner_reversion_strategy, self).OnStarted(time)

        # Create indicators
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_period
        
        atr = AverageTrueRange()
        atr.Length = self.atr_period

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, atr, self.ProcessCandle).Start()

        # Configure chart
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawIndicator(area, atr)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, ema_value, atr_value):
        """
        Process candle and check for Keltner Channel signals
        
        :param candle: The processed candle message.
        :param ema_value: The current value of the EMA indicator.
        :param atr_value: The current value of the ATR indicator.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Convert values to float
        ema_decimal = float(ema_value)
        atr_decimal = float(atr_value)

        # Calculate Keltner Channel bands
        upper_band = ema_decimal + (atr_decimal * self.atr_multiplier)
        lower_band = ema_decimal - (atr_decimal * self.atr_multiplier)
        
        # Calculate stop-loss amount based on ATR
        stop_loss_amount = atr_decimal * self.stop_loss_atr_multiplier

        if self.Position == 0:
            # No position - check for entry signals
            if candle.ClosePrice < lower_band:
                # Price is below lower band - buy (long)
                self.BuyMarket(self.Volume)
                self.LogInfo("Buy signal: Price {0} below lower Keltner band {1:F2}".format(
                    candle.ClosePrice, lower_band))
            elif candle.ClosePrice > upper_band:
                # Price is above upper band - sell (short)
                self.SellMarket(self.Volume)
                self.LogInfo("Sell signal: Price {0} above upper Keltner band {1:F2}".format(
                    candle.ClosePrice, upper_band))
        elif self.Position > 0:
            # Long position - check for exit signal
            if candle.ClosePrice > ema_decimal:
                # Price has returned to or above EMA - exit long
                self.SellMarket(self.Position)
                self.LogInfo("Exit long: Price {0} returned above EMA {1:F2}".format(
                    candle.ClosePrice, ema_decimal))
        elif self.Position < 0:
            # Short position - check for exit signal
            if candle.ClosePrice < ema_decimal:
                # Price has returned to or below EMA - exit short
                self.BuyMarket(Math.Abs(self.Position))
                self.LogInfo("Exit short: Price {0} returned below EMA {1:F2}".format(
                    candle.ClosePrice, ema_decimal))

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return keltner_reversion_strategy()
