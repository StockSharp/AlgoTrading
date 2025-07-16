import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import DataType, UnitTypes, Unit
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import WilliamsR
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class williams_percent_r_strategy(Strategy):
    """
    Strategy based on Williams %R indicator.
    It uses Williams %R overbought/oversold levels to generate trading signals.
    Williams %R values are negative, typically from 0 to -100.
    
    See more examples: https://github.com/StockSharp/AlgoTrading
    """
    
    def __init__(self):
        super(williams_percent_r_strategy, self).__init__()
        
        # Initialize strategy parameters
        self._period = self.Param("Period", 14) \
            .SetDisplay("Period", "Period for Williams %R calculation", "Indicators")
        
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")
        
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def period(self):
        """Williams %R period."""
        return self._period.Value

    @period.setter
    def period(self, value):
        self._period.Value = value

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
        super(williams_percent_r_strategy, self).OnReseted()

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(williams_percent_r_strategy, self).OnStarted(time)

        # Create Williams %R indicator
        williams_r = WilliamsR()
        williams_r.Length = self.period

        # Subscribe to candles and bind the indicator
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(williams_r, self.ProcessCandle).Start()
            
        # Enable position protection
        self.StartProtection(None, Unit(self.stop_loss_percent, UnitTypes.Percent))

        # Setup chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, williams_r)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, value):
        """
        Processes each finished candle and executes Williams %R-based trading logic.
        
        :param candle: The processed candle message.
        :param value: The current value of the Williams %R indicator.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        williams_r_value = float(value)

        # Note: Williams %R values are negative, typically from 0 to -100
        # Oversold: Below -80
        # Overbought: Above -20

        # Entry logic
        if williams_r_value < -80 and self.Position <= 0:
            # Oversold condition - buy signal
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)
            self.LogInfo("Buy signal: Williams %R oversold at {0}".format(williams_r_value))
        elif williams_r_value > -20 and self.Position >= 0:
            # Overbought condition - sell signal
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)
            self.LogInfo("Sell signal: Williams %R overbought at {0}".format(williams_r_value))

        # Exit logic
        if self.Position > 0 and williams_r_value > -50:
            # Exit long position when returning to neutral territory
            self.SellMarket(Math.Abs(self.Position))
            self.LogInfo("Exiting long position: Williams %R at {0}".format(williams_r_value))
        elif self.Position < 0 and williams_r_value < -50:
            # Exit short position when returning to neutral territory
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo("Exiting short position: Williams %R at {0}".format(williams_r_value))

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return williams_percent_r_strategy()