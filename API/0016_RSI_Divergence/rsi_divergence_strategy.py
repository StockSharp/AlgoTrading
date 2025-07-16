import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import DataType, UnitTypes, Unit
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class rsi_divergence_strategy(Strategy):
    """
    Strategy based on RSI divergence.
    It detects bullish divergence (price makes lower low, RSI makes higher low) 
    and bearish divergence (price makes higher high, RSI makes lower high).
    
    See more examples: https://github.com/StockSharp/AlgoTrading
    """
    
    def __init__(self):
        super(rsi_divergence_strategy, self).__init__()
        
        # Initialize strategy parameters
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "Period for RSI calculation", "Indicators")
        
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")
        
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        
        # State tracking
        self._prev_price = 0.0
        self._prev_rsi = 0.0
        self._is_first_candle = True

    @property
    def rsi_period(self):
        """RSI period."""
        return self._rsi_period.Value

    @rsi_period.setter
    def rsi_period(self, value):
        self._rsi_period.Value = value

    @property
    def stop_loss_percent(self):
        """Stop loss percentage."""
        return self._stop_loss_percent.Value

    @stop_loss_percent.setter
    def stop_loss_percent(self, value):
        self._stop_loss_percent.Value = value

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
        super(rsi_divergence_strategy, self).OnReseted()
        self._prev_price = 0.0
        self._prev_rsi = 0.0
        self._is_first_candle = True

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(rsi_divergence_strategy, self).OnStarted(time)

        # Reset state variables
        self._prev_price = 0.0
        self._prev_rsi = 0.0
        self._is_first_candle = True

        # Create RSI indicator
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period

        # Subscribe to candles and bind the indicator
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, self.ProcessCandle).Start()

        # Enable position protection
        self.StartProtection(None, Unit(self.stop_loss_percent, UnitTypes.Percent))

        # Setup chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, rsi_value):
        """
        Processes each finished candle and executes RSI divergence logic.
        
        :param candle: The processed candle message.
        :param rsi_value: The current value of the RSI indicator.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        current_price = candle.ClosePrice
        current_rsi = float(rsi_value)

        # For the first candle, just store values and return
        if self._is_first_candle:
            self._prev_price = current_price
            self._prev_rsi = current_rsi
            self._is_first_candle = False
            return

        # Detect bullish divergence: Price makes lower low but RSI makes higher low
        if (current_price < self._prev_price and 
            current_rsi > self._prev_rsi and self.Position <= 0):
            # Buy signal
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)
            self.LogInfo("Bullish divergence detected: Price {0} -> {1}, RSI {2} -> {3}".format(
                self._prev_price, current_price, self._prev_rsi, current_rsi))

        # Detect bearish divergence: Price makes higher high but RSI makes lower high
        elif (current_price > self._prev_price and 
              current_rsi < self._prev_rsi and self.Position >= 0):
            # Sell signal
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)
            self.LogInfo("Bearish divergence detected: Price {0} -> {1}, RSI {2} -> {3}".format(
                self._prev_price, current_price, self._prev_rsi, current_rsi))

        # Exit logic for long positions: RSI crosses above 70 (overbought)
        if self.Position > 0 and current_rsi > 70:
            self.SellMarket(Math.Abs(self.Position))
            self.LogInfo("Exiting long position: RSI overbought at {0}".format(current_rsi))

        # Exit logic for short positions: RSI crosses below 30 (oversold)
        elif self.Position < 0 and current_rsi < 30:
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo("Exiting short position: RSI oversold at {0}".format(current_rsi))

        # Update previous values for next comparison
        self._prev_price = current_price
        self._prev_rsi = current_rsi

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return rsi_divergence_strategy()
