import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import DataType, UnitTypes, Unit
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class cci_breakout_strategy(Strategy):
    """
    Strategy based on CCI (Commodity Channel Index) breakout.
    It enters long when CCI breaks above +100 (strong upward momentum)
    and short when CCI breaks below -100 (strong downward momentum).
    
    See more examples: https://github.com/StockSharp/AlgoTrading
    """
    
    def __init__(self):
        super(cci_breakout_strategy, self).__init__()
        
        # Initialize strategy parameters
        self._cci_period = self.Param("CciPeriod", 20) \
            .SetDisplay("CCI Period", "Period for CCI calculation", "Indicators")
        
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")
        
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def cci_period(self):
        """CCI period."""
        return self._cci_period.Value

    @cci_period.setter
    def cci_period(self, value):
        self._cci_period.Value = value

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
        super(cci_breakout_strategy, self).OnReseted()

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(cci_breakout_strategy, self).OnStarted(time)

        # Create CCI indicator
        cci = CommodityChannelIndex()
        cci.Length = self.cci_period

        # Subscribe to candles and bind the indicator
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(cci, self.ProcessCandle).Start()

        # Enable stop loss protection
        self.StartProtection(None, Unit(self.stop_loss_percent, UnitTypes.Percent))

        # Setup chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, cci)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, value):
        """
        Processes each finished candle and executes CCI breakout logic.
        
        :param candle: The processed candle message.
        :param value: The current value of the CCI indicator.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        cci_value = float(value)

        # Entry logic
        if cci_value > 100 and self.Position <= 0:
            # CCI breakout above +100 - Strong upward momentum
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)
            self.LogInfo("Buy signal: CCI breakout above +100. Value: {0:F2}".format(cci_value))
        elif cci_value < -100 and self.Position >= 0:
            # CCI breakout below -100 - Strong downward momentum
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)
            self.LogInfo("Sell signal: CCI breakout below -100. Value: {0:F2}".format(cci_value))

        # Exit logic
        if self.Position > 0 and cci_value < 0:
            # Exit long position when CCI crosses back below zero
            self.SellMarket(Math.Abs(self.Position))
            self.LogInfo("Exiting long position: CCI crossed below zero. Value: {0:F2}".format(cci_value))
        elif self.Position < 0 and cci_value > 0:
            # Exit short position when CCI crosses back above zero
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo("Exiting short position: CCI crossed above zero. Value: {0:F2}".format(cci_value))

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return cci_breakout_strategy()
