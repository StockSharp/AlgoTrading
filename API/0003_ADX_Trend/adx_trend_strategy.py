import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import DataType, UnitTypes, Unit
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import AverageDirectionalIndex, SimpleMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class adx_trend_strategy(Strategy):
    """
    Strategy based on Average Directional Index (ADX) trend.
    It enters long position when ADX > 25 and price > MA, 
    and short position when ADX > 25 and price < MA.
    
    See more examples: https://github.com/StockSharp/AlgoTrading
    """
    
    def __init__(self):
        super(adx_trend_strategy, self).__init__()
        
        # Initialize strategy parameters
        self._adx_period = self.Param("AdxPeriod", 14) \
            .SetDisplay("ADX Period", "Period for calculating ADX indicator", "Indicators")
        
        self._ma_period = self.Param("MaPeriod", 50) \
            .SetDisplay("MA Period", "Period for calculating Moving Average", "Indicators")
        
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0) \
            .SetDisplay("ATR Multiplier", "Multiplier for stop-loss based on ATR", "Risk parameters")
        
        self._adx_exit_threshold = self.Param("AdxExitThreshold", 20) \
            .SetDisplay("ADX Exit Threshold", "ADX level below which to exit position", "Exit parameters")
        
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        
        # Current trend state
        self._adx_above_threshold = False
        self._prev_adx_value = 0.0
        self._prev_ma_value = 0.0

    @property
    def adx_period(self):
        """ADX period."""
        return self._adx_period.Value

    @adx_period.setter
    def adx_period(self, value):
        self._adx_period.Value = value

    @property
    def ma_period(self):
        """Moving Average period."""
        return self._ma_period.Value

    @ma_period.setter
    def ma_period(self, value):
        self._ma_period.Value = value

    @property
    def atr_multiplier(self):
        """ATR multiplier for stop loss."""
        return self._atr_multiplier.Value

    @atr_multiplier.setter
    def atr_multiplier(self, value):
        self._atr_multiplier.Value = value

    @property
    def adx_exit_threshold(self):
        """ADX threshold to exit position."""
        return self._adx_exit_threshold.Value

    @adx_exit_threshold.setter
    def adx_exit_threshold(self, value):
        self._adx_exit_threshold.Value = value

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
        super(adx_trend_strategy, self).OnReseted()
        self._adx_above_threshold = False
        self._prev_adx_value = 0.0
        self._prev_ma_value = 0.0

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(adx_trend_strategy, self).OnStarted(time)

        # Initialize state
        self._adx_above_threshold = False
        self._prev_adx_value = 0.0
        self._prev_ma_value = 0.0

        # Create indicators
        adx = AverageDirectionalIndex()
        adx.Length = self.adx_period
        
        ma = SimpleMovingAverage()
        ma.Length = self.ma_period
        
        atr = AverageTrueRange()
        atr.Length = self.adx_period

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(adx, ma, atr, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, adx)
            self.DrawIndicator(area, ma)
            self.DrawOwnTrades(area)

        # Start protection for positions
        self.StartProtection(None, Unit(self.atr_multiplier, UnitTypes.Absolute))

    def ProcessCandle(self, candle, adx_value, ma_value, atr_value):
        """
        Processes each finished candle and executes ADX-based trading logic.
        
        :param candle: The processed candle message.
        :param adx_value: The current value of the ADX indicator.
        :param ma_value: The current value of the moving average.
        :param atr_value: The current value of the ATR indicator.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Extract ADX moving average value
        # In Python, we need to handle the ADX value properly
        try:
            # ADX indicator returns AverageDirectionalIndexValue with MovingAverage property
            if hasattr(adx_value, 'MovingAverage') and adx_value.MovingAverage is not None:
                adx_ma = float(adx_value.MovingAverage)
            else:
                # Fallback to direct value if MovingAverage is not available
                adx_ma = float(adx_value)
        except:
            # If we can't get ADX value, skip this candle
            return

        # Convert ma_value to decimal
        ma_decimal = float(ma_value)

        # Check ADX threshold for entry conditions
        is_adx_enough_for_entry = adx_ma > 25
        
        # Check ADX threshold for exit conditions
        is_adx_below_exit = adx_ma < self.adx_exit_threshold
        
        # Current price relative to MA
        is_price_above_ma = candle.ClosePrice > ma_decimal

        # Store ADX state
        self._adx_above_threshold = is_adx_enough_for_entry

        # Trading logic
        if is_adx_below_exit and self.Position != 0:
            # Exit position when ADX weakens
            self.ClosePosition()
            self.LogInfo("Exiting position at {0}. ADX = {1} (below threshold {2})".format(
                candle.ClosePrice, adx_ma, self.adx_exit_threshold))
        elif is_adx_enough_for_entry:
            volume = self.Volume + Math.Abs(self.Position)

            # Long entry
            if is_price_above_ma and self.Position <= 0:
                self.BuyMarket(volume)
                self.LogInfo("Buy signal: ADX = {0}, Price = {1}, MA = {2}".format(
                    adx_ma, candle.ClosePrice, ma_value))
            # Short entry
            elif not is_price_above_ma and self.Position >= 0:
                self.SellMarket(volume)
                self.LogInfo("Sell signal: ADX = {0}, Price = {1}, MA = {2}".format(
                    adx_ma, candle.ClosePrice, ma_value))

        # Update previous values
        self._prev_adx_value = adx_ma
        self._prev_ma_value = ma_decimal

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return adx_trend_strategy()
