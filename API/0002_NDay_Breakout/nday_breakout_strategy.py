import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import DataType
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest, SMA
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class nday_breakout_strategy(Strategy):
    """
    N-day high/low breakout strategy.
    Enters long when price breaks above the N-day high.
    Enters short when price breaks below the N-day low.
    Exits when price crosses the moving average.
    
    """
    
    def __init__(self):
        super(nday_breakout_strategy, self).__init__()
        
        # Initialize strategy parameters
        self._lookback_period = self.Param("LookbackPeriod", 20) \
            .SetDisplay("Lookback Period", "Number of days to determine the high/low range", "Strategy Parameters")
        
        self._ma_period = self.Param("MaPeriod", 20) \
            .SetDisplay("MA Period", "Period for the moving average used as exit signal", "Strategy Parameters")
        
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management")
        
        self._candle_type = self.Param("CandleType", tf(1*1440)) \
            .SetDisplay("Candle Type", "Type of candles to use", "Strategy Parameters")
        
        # Initialize indicators (will be created in OnStarted)
        self._highest = None
        self._lowest = None
        self._ma = None
        
        # Values for tracking breakouts
        self._n_day_high = 0.0
        self._n_day_low = float('inf')
        self._is_formed = False

    @property
    def lookback_period(self):
        """Period for looking back to determine the highest/lowest value."""
        return self._lookback_period.Value

    @lookback_period.setter
    def lookback_period(self, value):
        self._lookback_period.Value = value

    @property
    def ma_period(self):
        """Period for the moving average used for exit signals."""
        return self._ma_period.Value

    @ma_period.setter
    def ma_period(self, value):
        self._ma_period.Value = value

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
        super(nday_breakout_strategy, self).OnReseted()
        self._n_day_high = 0.0
        self._n_day_low = float('inf')
        self._is_formed = False

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(nday_breakout_strategy, self).OnStarted(time)

        # Initialize tracking variables
        self._n_day_high = 0.0
        self._n_day_low = float('inf')
        self._is_formed = False

        # Create indicators
        self._highest = Highest()
        self._highest.Length = self.lookback_period
        
        self._lowest = Lowest()
        self._lowest.Length = self.lookback_period
        
        self._ma = SMA()
        self._ma.Length = self.ma_period

        # Create subscription for candles
        subscription = self.SubscribeCandles(self.candle_type)
        
        # Bind indicators to candles
        subscription.Bind(self._highest, self._lowest, self._ma, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._highest)
            self.DrawIndicator(area, self._lowest)
            self.DrawIndicator(area, self._ma)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, highest_value, lowest_value, ma_value):
        """
        Processes each finished candle and executes breakout trading logic.
        
        :param candle: The processed candle message.
        :param highest_value: The current value of the highest indicator.
        :param lowest_value: The current value of the lowest indicator.
        :param ma_value: The current value of the moving average.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Wait until indicators are formed
        if not self._is_formed:
            # Check if highest and lowest indicators are now formed
            if self._highest.IsFormed and self._lowest.IsFormed:
                self._n_day_high = highest_value
                self._n_day_low = lowest_value
                self._is_formed = True
                self.LogInfo("Indicators formed. Initial N-day high: {0}, N-day low: {1}".format(
                    self._n_day_high, self._n_day_low))
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        self.LogInfo("Processing candle: High={0}, Low={1}, Close={2}".format(
            candle.HighPrice, candle.LowPrice, candle.ClosePrice))
        self.LogInfo("Current N-day high: {0}, N-day low: {1}, MA: {2}".format(
            self._n_day_high, self._n_day_low, ma_value))

        # Entry logic - only trigger on breakouts
        if candle.HighPrice > self._n_day_high and self.Position <= 0:
            # Long entry - price breaks above the N-day high
            self.LogInfo("Long entry signal: Price {0} broke above N-day high {1}".format(
                candle.HighPrice, self._n_day_high))
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)
        elif candle.LowPrice < self._n_day_low and self.Position >= 0:
            # Short entry - price breaks below the N-day low
            self.LogInfo("Short entry signal: Price {0} broke below N-day low {1}".format(
                candle.LowPrice, self._n_day_low))
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)

        # Exit logic
        if self.Position > 0 and candle.ClosePrice < ma_value:
            # Exit long position when price crosses below MA
            self.LogInfo("Long exit signal: Price {0} crossed below MA {1}".format(
                candle.ClosePrice, ma_value))
            self.SellMarket(Math.Abs(self.Position))
        elif self.Position < 0 and candle.ClosePrice > ma_value:
            # Exit short position when price crosses above MA
            self.LogInfo("Short exit signal: Price {0} crossed above MA {1}".format(
                candle.ClosePrice, ma_value))
            self.BuyMarket(Math.Abs(self.Position))

        # Update N-day high and low values for next candle
        self._n_day_high = highest_value
        self._n_day_low = lowest_value

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return nday_breakout_strategy()
