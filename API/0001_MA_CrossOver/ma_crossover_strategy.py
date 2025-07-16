import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import DataType
from StockSharp.Messages import CandleStates
from StockSharp.Messages import Sides
from StockSharp.Algo.Indicators import SMA
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class ma_crossover_strategy(Strategy):
    """
    Moving average crossover strategy.
    Enters long when fast MA crosses above slow MA.
    Enters short when fast MA crosses below slow MA.
    Implements stop-loss as a percentage of entry price.
    
    """
    
    def __init__(self):
        super(ma_crossover_strategy, self).__init__()
        
        # Initialize strategy parameters
        self._fast_length = self.Param("FastLength", 10) \
            .SetDisplay("Fast MA Length", "Period of the fast moving average", "MA Settings")
        
        self._slow_length = self.Param("SlowLength", 50) \
            .SetDisplay("Slow MA Length", "Period of the slow moving average", "MA Settings")
        
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management")
        
        self._candle_type = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        
        # Initialize state variables
        self._entry_price = 0.0
        self._is_long_position = False
        self._previous_fast_value = 0.0
        self._previous_slow_value = 0.0
        self._was_fast_less_than_slow = False
        self._is_initialized = False

    @property
    def fast_length(self):
        """Fast MA period length."""
        return self._fast_length.Value

    @fast_length.setter
    def fast_length(self, value):
        self._fast_length.Value = value

    @property
    def slow_length(self):
        """Slow MA period length."""
        return self._slow_length.Value

    @slow_length.setter
    def slow_length(self, value):
        self._slow_length.Value = value

    @property
    def stop_loss_percent(self):
        """Stop-loss percentage."""
        return self._stop_loss_percent.Value

    @stop_loss_percent.setter
    def stop_loss_percent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def candle_type(self):
        """The type of candles to use for strategy calculation."""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        """
        Resets internal state when strategy is reset.
        """
        super(ma_crossover_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._is_long_position = False
        self._previous_fast_value = 0.0
        self._previous_slow_value = 0.0
        self._was_fast_less_than_slow = False
        self._is_initialized = False

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(ma_crossover_strategy, self).OnStarted(time)

        # Initialize state variables
        self._entry_price = 0.0
        self._is_long_position = False
        self._is_initialized = False

        # Create indicators
        fast_ma = SMA()
        fast_ma.Length = self.fast_length
        slow_ma = SMA()
        slow_ma.Length = self.slow_length

        # Create subscription for candles
        subscription = self.SubscribeCandles(self.candle_type)
        
        # Bind indicators to the candles and start processing
        subscription.Bind(fast_ma, slow_ma, self.OnProcess).Start()

        # Configure chart if GUI is available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ma)
            self.DrawIndicator(area, slow_ma)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, fast_value, slow_value):
        """
        Processes each finished candle and executes trading logic on MA crossover.
        
        :param candle: The processed candle message.
        :param fast_value: The current value of the fast MA.
        :param slow_value: The current value of the slow MA.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Initialize on first complete values
        if not self._is_initialized and fast_value != 0 and slow_value != 0:
            self._previous_fast_value = fast_value
            self._previous_slow_value = slow_value
            self._was_fast_less_than_slow = fast_value < slow_value
            self._is_initialized = True
            self.LogInfo("Strategy initialized. Fast MA: {0}, Slow MA: {1}".format(fast_value, slow_value))
            return

        if not self._is_initialized:
            return

        # Current crossover state
        is_fast_less_than_slow = fast_value < slow_value
        
        self.LogInfo("Candle: {0}, Close: {1}, Fast MA: {2}, Slow MA: {3}".format(
            candle.OpenTime, candle.ClosePrice, fast_value, slow_value))
        
        # Check for crossovers
        if self._was_fast_less_than_slow != is_fast_less_than_slow:
            # Crossover happened
            if not is_fast_less_than_slow:  # Fast MA crossed above Slow MA
                # Buy signal
                if self.Position <= 0:
                    self._entry_price = candle.ClosePrice
                    self._is_long_position = True
                    volume = self.Volume + Math.Abs(self.Position)
                    self.BuyMarket(volume)
                    self.LogInfo("Long entry: Fast MA {0} crossed above Slow MA {1}".format(fast_value, slow_value))
            else:  # Fast MA crossed below Slow MA
                # Sell signal
                if self.Position >= 0:
                    self._entry_price = candle.ClosePrice
                    self._is_long_position = False
                    volume = self.Volume + Math.Abs(self.Position)
                    self.SellMarket(volume)
                    self.LogInfo("Short entry: Fast MA {0} crossed below Slow MA {1}".format(fast_value, slow_value))
            
            # Update the crossover state
            self._was_fast_less_than_slow = is_fast_less_than_slow

        # Check stop-loss conditions
        if self.Position != 0 and self._entry_price != 0:
            self._check_stop_loss(candle.ClosePrice)

        # Update previous values
        self._previous_fast_value = fast_value
        self._previous_slow_value = slow_value

    def _check_stop_loss(self, current_price):
        """
        Checks and executes stop-loss logic.
        
        :param current_price: Current market price.
        """
        if self._entry_price == 0:
            return

        stop_loss_threshold = self.stop_loss_percent / 100.0

        if self._is_long_position and self.Position > 0:
            # For long positions, exit if price falls below entry price - stop percentage
            stop_price = self._entry_price * (1.0 - stop_loss_threshold)
            if current_price <= stop_price:
                self.SellMarket(Math.Abs(self.Position))
                self.LogInfo("Long stop-loss triggered at {0}. Entry was {1}, Stop level: {2}".format(
                    current_price, self._entry_price, stop_price))
        elif not self._is_long_position and self.Position < 0:
            # For short positions, exit if price rises above entry price + stop percentage
            stop_price = self._entry_price * (1.0 + stop_loss_threshold)
            if current_price >= stop_price:
                self.BuyMarket(Math.Abs(self.Position))
                self.LogInfo("Short stop-loss triggered at {0}. Entry was {1}, Stop level: {2}".format(
                    current_price, self._entry_price, stop_price))

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return ma_crossover_strategy()
