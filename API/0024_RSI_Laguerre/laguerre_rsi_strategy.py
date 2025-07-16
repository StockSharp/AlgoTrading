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

class laguerre_rsi_strategy(Strategy):
    """
    Strategy based on Laguerre RSI.
    Note: Since StockSharp doesn't have built-in Laguerre RSI, this implementation 
    uses regular RSI but applies Laguerre RSI trading logic with 0-1 scale.
    
    See more examples: https://github.com/StockSharp/AlgoTrading
    """
    
    def __init__(self):
        super(laguerre_rsi_strategy, self).__init__()
        
        # Initialize strategy parameters
        self._gamma = self.Param("Gamma", 0.7) \
            .SetDisplay("Gamma", "Gamma parameter for Laguerre RSI", "Indicators")
        
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")
        
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def gamma(self):
        """Laguerre RSI Gamma parameter."""
        return self._gamma.Value

    @gamma.setter
    def gamma(self, value):
        self._gamma.Value = value

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
        super(laguerre_rsi_strategy, self).OnReseted()

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(laguerre_rsi_strategy, self).OnStarted(time)

        # Create Laguerre RSI indicator
        # Note: StockSharp doesn't have a built-in Laguerre RSI, so we'll use a regular RSI
        # but apply the strategy logic for Laguerre RSI
        rsi = RelativeStrengthIndex()
        rsi.Length = 14

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
        Processes each finished candle and executes Laguerre RSI logic.
        
        :param candle: The processed candle message.
        :param rsi_value: The current value of the RSI indicator.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Get RSI value and normalize it to 0-1 range (Laguerre RSI uses 0-1 scale)
        rsi = float(rsi_value)
        norm_rsi = rsi / 100.0  # Convert standard RSI (0-100) to Laguerre RSI scale (0-1)

        # Get price direction
        is_price_rising = candle.OpenPrice < candle.ClosePrice

        # Entry logic based on Laguerre RSI levels
        # - Buy when RSI is below 0.2 (oversold) and price is rising
        # - Sell when RSI is above 0.8 (overbought) and price is falling
        if norm_rsi < 0.2 and is_price_rising and self.Position <= 0:
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)
            self.LogInfo("Buy signal: Laguerre RSI oversold at {0:F4} with rising price".format(norm_rsi))
        elif norm_rsi > 0.8 and not is_price_rising and self.Position >= 0:
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)
            self.LogInfo("Sell signal: Laguerre RSI overbought at {0:F4} with falling price".format(norm_rsi))

        # Exit logic
        if self.Position > 0 and norm_rsi > 0.8:
            # Exit long when RSI reaches overbought
            self.SellMarket(Math.Abs(self.Position))
            self.LogInfo("Exiting long position: Laguerre RSI reached overbought at {0:F4}".format(norm_rsi))
        elif self.Position < 0 and norm_rsi < 0.2:
            # Exit short when RSI reaches oversold
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo("Exiting short position: Laguerre RSI reached oversold at {0:F4}".format(norm_rsi))

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return laguerre_rsi_strategy()
