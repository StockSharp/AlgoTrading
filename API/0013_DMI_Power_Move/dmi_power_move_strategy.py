import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, UnitTypes, Unit, CandleStates
from StockSharp.Algo.Indicators import AverageDirectionalIndex, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class dmi_power_move_strategy(Strategy):
    """
    Strategy based on DMI (Directional Movement Index) power moves.
    It enters long position when +DI exceeds -DI by a specified threshold and ADX is strong.
    It enters short position when -DI exceeds +DI by a specified threshold and ADX is strong.
    
    """
    
    def __init__(self):
        super(dmi_power_move_strategy, self).__init__()
        
        # Initialize strategy parameters
        self._dmi_period = self.Param("DmiPeriod", 14) \
            .SetDisplay("DMI Period", "Period for Directional Movement Index calculation", "Indicators")
        
        self._di_difference_threshold = self.Param("DiDifferenceThreshold", 5.0) \
            .SetDisplay("DI Difference Threshold", "Minimum difference between +DI and -DI for signal", "Trading parameters")
        
        self._adx_threshold = self.Param("AdxThreshold", 30.0) \
            .SetDisplay("ADX Threshold", "Minimum ADX value to consider trend strong", "Trading parameters")
        
        self._adx_exit_threshold = self.Param("AdxExitThreshold", 25.0) \
            .SetDisplay("ADX Exit Threshold", "ADX value below which to exit positions", "Exit parameters")
        
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0) \
            .SetDisplay("ATR Multiplier", "Multiplier for ATR to determine stop-loss distance", "Risk parameters")
        
        self._candle_type = self.Param("CandleType", tf(15)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def dmi_period(self):
        """Period for DMI calculation."""
        return self._dmi_period.Value

    @dmi_period.setter
    def dmi_period(self, value):
        self._dmi_period.Value = value

    @property
    def di_difference_threshold(self):
        """Minimum difference between +DI and -DI to generate a signal."""
        return self._di_difference_threshold.Value

    @di_difference_threshold.setter
    def di_difference_threshold(self, value):
        self._di_difference_threshold.Value = value

    @property
    def adx_threshold(self):
        """Minimum ADX value to consider trend strong enough for entry."""
        return self._adx_threshold.Value

    @adx_threshold.setter
    def adx_threshold(self, value):
        self._adx_threshold.Value = value

    @property
    def adx_exit_threshold(self):
        """ADX value below which to exit positions."""
        return self._adx_exit_threshold.Value

    @adx_exit_threshold.setter
    def adx_exit_threshold(self, value):
        self._adx_exit_threshold.Value = value

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
        super(dmi_power_move_strategy, self).OnReseted()

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(dmi_power_move_strategy, self).OnStarted(time)

        # Create indicators
        dmi = AverageDirectionalIndex()
        dmi.Length = self.dmi_period
        
        atr = AverageTrueRange()
        atr.Length = self.dmi_period

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(dmi, atr, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, dmi)
            self.DrawOwnTrades(area)

        # Start protection with ATR-based stop loss
        self.StartProtection(
            takeProfit=None,
            stopLoss=Unit(self.atr_multiplier, UnitTypes.Absolute)
        )
    def ProcessCandle(self, candle, adx_value, atr_value):
        """
        Processes each finished candle and executes DMI-based trading logic.
        
        :param candle: The processed candle message.
        :param adx_value: The current value of the ADX indicator.
        :param atr_value: The current value of the ATR indicator.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Extract ADX and DI values
        if adx_value.MovingAverage is None:
            return
        if adx_value.Dx is None or adx_value.Dx.Plus is None or adx_value.Dx.Minus is None:
            return
        adx = float(adx_value.MovingAverage)
        plus_di_value = float(adx_value.Dx.Plus)
        minus_di_value = float(adx_value.Dx.Minus)

        # Calculate the difference between +DI and -DI
        di_difference = plus_di_value - minus_di_value
        
        # Check trading conditions
        is_strong_bullish_trend = (di_difference > self.di_difference_threshold and 
                                 adx > self.adx_threshold)
        is_strong_bearish_trend = (di_difference < -self.di_difference_threshold and 
                                 adx > self.adx_threshold)
        is_weak_trend = adx < self.adx_exit_threshold
        
        # Entry logic
        if is_strong_bullish_trend and self.Position <= 0:
            # Strong bullish trend - Buy signal
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)
            self.LogInfo("Buy signal: +DI - (-DI) = {0}, ADX = {1}".format(
                di_difference, adx))
        elif is_strong_bearish_trend and self.Position >= 0:
            # Strong bearish trend - Sell signal
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)
            self.LogInfo("Sell signal: -DI - (+DI) = {0}, ADX = {1}".format(
                -di_difference, adx))
        # Exit logic
        elif is_weak_trend and self.Position != 0:
            # Trend is weakening - Exit position
            self.ClosePosition()
            self.LogInfo("Exit signal: ADX = {0} (below threshold {1})".format(
                adx, self.adx_exit_threshold))

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return dmi_power_move_strategy()