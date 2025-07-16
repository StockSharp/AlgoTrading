import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import DataType
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class low_vol_reversion_strategy(Strategy):
    """
    Strategy that trades on mean reversion during periods of low volatility.
    It identifies periods of low ATR (Average True Range) and opens positions when price
    deviates from its moving average, expecting a return to the mean.
    
    See more examples: https://github.com/StockSharp/AlgoTrading
    """
    
    def __init__(self):
        super(low_vol_reversion_strategy, self).__init__()
        
        # Initialize strategy parameters
        self._ma_period = self.Param("MAPeriod", 20) \
            .SetDisplay("MA Period", "Period for Moving Average calculation", "Technical Parameters")
        
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "Period for ATR calculation", "Technical Parameters")
        
        self._atr_lookback_period = self.Param("AtrLookbackPeriod", 20) \
            .SetDisplay("ATR Lookback Period", "Lookback period for ATR average calculation", "Technical Parameters")
        
        self._atr_threshold_percent = self.Param("AtrThresholdPercent", 50.0) \
            .SetDisplay("ATR Threshold %", "ATR threshold as percentage of average ATR", "Entry Parameters")
        
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0) \
            .SetDisplay("ATR Multiplier", "ATR multiplier for stop-loss calculation", "Risk Management")
        
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "Data")
        
        # State tracking
        self._avg_atr = 0.0
        self._lookback_counter = 0

    @property
    def ma_period(self):
        """Period for Moving Average calculation (default: 20)"""
        return self._ma_period.Value

    @ma_period.setter
    def ma_period(self, value):
        self._ma_period.Value = value

    @property
    def atr_period(self):
        """Period for ATR calculation (default: 14)"""
        return self._atr_period.Value

    @atr_period.setter
    def atr_period(self, value):
        self._atr_period.Value = value

    @property
    def atr_lookback_period(self):
        """Lookback period for ATR average calculation (default: 20)"""
        return self._atr_lookback_period.Value

    @atr_lookback_period.setter
    def atr_lookback_period(self, value):
        self._atr_lookback_period.Value = value

    @property
    def atr_threshold_percent(self):
        """ATR threshold as percentage of average ATR (default: 50%)"""
        return self._atr_threshold_percent.Value

    @atr_threshold_percent.setter
    def atr_threshold_percent(self, value):
        self._atr_threshold_percent.Value = value

    @property
    def atr_multiplier(self):
        """ATR multiplier for stop-loss calculation (default: 2.0)"""
        return self._atr_multiplier.Value

    @atr_multiplier.setter
    def atr_multiplier(self, value):
        self._atr_multiplier.Value = value

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
        super(low_vol_reversion_strategy, self).OnReseted()
        self._avg_atr = 0.0
        self._lookback_counter = 0

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(low_vol_reversion_strategy, self).OnStarted(time)

        # Reset state variables
        self._avg_atr = 0.0
        self._lookback_counter = 0

        # Create indicators
        sma = SimpleMovingAverage()
        sma.Length = self.ma_period
        
        atr = AverageTrueRange()
        atr.Length = self.atr_period

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, atr, self.ProcessCandle).Start()

        # Configure chart
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawIndicator(area, atr)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, sma_value, atr_value):
        """
        Process candle and check for low volatility mean reversion signals
        
        :param candle: The processed candle message.
        :param sma_value: The current value of the SMA indicator.
        :param atr_value: The current value of the ATR indicator.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Convert values to float
        sma_decimal = float(sma_value)
        atr_decimal = float(atr_value)

        # Gather ATR values for average calculation
        if self._lookback_counter < self.atr_lookback_period:
            # Still collecting ATR values for the average
            if self._lookback_counter == 0:
                self._avg_atr = atr_decimal
            else:
                # Calculate running average
                self._avg_atr = (self._avg_atr * self._lookback_counter + atr_decimal) / (self._lookback_counter + 1)
            
            self._lookback_counter += 1
            return
        else:
            # Update running average
            self._avg_atr = (self._avg_atr * (self.atr_lookback_period - 1) + atr_decimal) / self.atr_lookback_period

        # Calculate ATR threshold
        atr_threshold = self._avg_atr * (self.atr_threshold_percent / 100)
        
        # Check if we're in a low volatility period
        is_low_volatility = atr_decimal < atr_threshold
        
        if not is_low_volatility:
            # Not a low volatility period, skip trading
            self.LogInfo("High volatility period: ATR {0:F4} > threshold {1:F4}".format(
                atr_decimal, atr_threshold))
            return

        # Calculate price deviation from MA
        is_price_above_ma = candle.ClosePrice > sma_decimal
        is_price_below_ma = candle.ClosePrice < sma_decimal
        
        # Calculate stop-loss amount based on ATR
        stop_loss_amount = atr_decimal * self.atr_multiplier

        if self.Position == 0:
            # No position - check for entry signals
            if is_price_below_ma:
                # Price is below MA in low volatility period - buy (long)
                self.BuyMarket(self.Volume)
                self.LogInfo("Buy signal: Low vol period, price {0} below MA {1:F2}. ATR: {2:F4}, Threshold: {3:F4}".format(
                    candle.ClosePrice, sma_decimal, atr_decimal, atr_threshold))
            elif is_price_above_ma:
                # Price is above MA in low volatility period - sell (short)
                self.SellMarket(self.Volume)
                self.LogInfo("Sell signal: Low vol period, price {0} above MA {1:F2}. ATR: {2:F4}, Threshold: {3:F4}".format(
                    candle.ClosePrice, sma_decimal, atr_decimal, atr_threshold))
        elif self.Position > 0:
            # Long position - check for exit signal
            if candle.ClosePrice > sma_decimal:
                # Price has reached MA - exit long
                self.SellMarket(self.Position)
                self.LogInfo("Exit long: Price {0} reached MA {1:F2}".format(
                    candle.ClosePrice, sma_decimal))
        elif self.Position < 0:
            # Short position - check for exit signal
            if candle.ClosePrice < sma_decimal:
                # Price has reached MA - exit short
                self.BuyMarket(Math.Abs(self.Position))
                self.LogInfo("Exit short: Price {0} reached MA {1:F2}".format(
                    candle.ClosePrice, sma_decimal))

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return low_vol_reversion_strategy()
