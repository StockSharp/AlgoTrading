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

class rsi_reversion_strategy(Strategy):
    """
    Strategy based on RSI mean reversion.
    It buys when RSI is oversold and sells when RSI is overbought,
    expecting prices to revert to the mean (exit level).
    
    See more examples: https://github.com/StockSharp/AlgoTrading
    """
    
    def __init__(self):
        super(rsi_reversion_strategy, self).__init__()
        
        # Initialize strategy parameters
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "Period for RSI calculation", "Indicators")
        
        self._oversold_threshold = self.Param("OversoldThreshold", 30.0) \
            .SetDisplay("Oversold Threshold", "RSI threshold for oversold condition", "Strategy")
        
        self._overbought_threshold = self.Param("OverboughtThreshold", 70.0) \
            .SetDisplay("Overbought Threshold", "RSI threshold for overbought condition", "Strategy")
        
        self._exit_level = self.Param("ExitLevel", 50.0) \
            .SetDisplay("Exit Level", "RSI level for exits (mean reversion)", "Strategy")
        
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")
        
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def rsi_period(self):
        """RSI period."""
        return self._rsi_period.Value

    @rsi_period.setter
    def rsi_period(self, value):
        self._rsi_period.Value = value

    @property
    def oversold_threshold(self):
        """RSI oversold threshold."""
        return self._oversold_threshold.Value

    @oversold_threshold.setter
    def oversold_threshold(self, value):
        self._oversold_threshold.Value = value

    @property
    def overbought_threshold(self):
        """RSI overbought threshold."""
        return self._overbought_threshold.Value

    @overbought_threshold.setter
    def overbought_threshold(self, value):
        self._overbought_threshold.Value = value

    @property
    def exit_level(self):
        """RSI exit level."""
        return self._exit_level.Value

    @exit_level.setter
    def exit_level(self, value):
        self._exit_level.Value = value

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
        super(rsi_reversion_strategy, self).OnReseted()

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(rsi_reversion_strategy, self).OnStarted(time)

        # Create RSI indicator
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period

        # Subscribe to candles
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(rsi, self.ProcessCandle).Start()

        # Enable position protection
        self.StartProtection(
            takeProfit=None,
            stopLoss=Unit(self.stop_loss_percent, UnitTypes.Percent)
        )
        # Setup chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, rsi_value):
        """
        Processes each finished candle and executes RSI mean reversion logic.
        
        :param candle: The processed candle message.
        :param rsi_value: The current value of the RSI indicator.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Get RSI value
        rsi = float(rsi_value)

        # Entry logic for mean reversion
        if rsi < self.oversold_threshold and self.Position <= 0:
            # Buy when RSI is oversold (below threshold)
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)
            self.LogInfo("Buy signal: RSI oversold at {0:F2}".format(rsi))
        elif rsi > self.overbought_threshold and self.Position >= 0:
            # Sell when RSI is overbought (above threshold)
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)
            self.LogInfo("Sell signal: RSI overbought at {0:F2}".format(rsi))

        # Exit logic based on RSI returning to mid-range (mean reversion)
        if self.Position > 0 and rsi > self.exit_level:
            # Exit long position when RSI returns to neutral zone
            self.SellMarket(Math.Abs(self.Position))
            self.LogInfo("Exiting long position: RSI returned to {0:F2}".format(rsi))
        elif self.Position < 0 and rsi < self.exit_level:
            # Exit short position when RSI returns to neutral zone
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo("Exiting short position: RSI returned to {0:F2}".format(rsi))

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return rsi_reversion_strategy()
