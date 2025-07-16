import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, UnitTypes, Unit, CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class bollinger_percent_b_strategy(Strategy):
    """
    Strategy that trades on Bollinger %B indicator.
    Bollinger %B shows where price is relative to the Bollinger Bands.
    Values below 0 or above 1 indicate price outside the bands.
    
    """
    
    def __init__(self):
        super(bollinger_percent_b_strategy, self).__init__()
        
        # Initialize strategy parameters
        self._bollinger_period = self.Param("BollingerPeriod", 20) \
            .SetDisplay("Bollinger Period", "Period for Bollinger Bands calculation", "Bollinger Parameters")
        
        self._bollinger_deviation = self.Param("BollingerDeviation", 2.0) \
            .SetDisplay("Bollinger Deviation", "Deviation for Bollinger Bands calculation", "Bollinger Parameters")
        
        self._exit_value = self.Param("ExitValue", 0.5) \
            .SetDisplay("Exit %B Value", "Exit threshold for %B", "Exit Parameters")
        
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss as percentage from entry price", "Risk Management")
        
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "Data")

    @property
    def bollinger_period(self):
        """Period for Bollinger Bands calculation (default: 20)"""
        return self._bollinger_period.Value

    @bollinger_period.setter
    def bollinger_period(self, value):
        self._bollinger_period.Value = value

    @property
    def bollinger_deviation(self):
        """Deviation for Bollinger Bands calculation (default: 2.0)"""
        return self._bollinger_deviation.Value

    @bollinger_deviation.setter
    def bollinger_deviation(self, value):
        self._bollinger_deviation.Value = value

    @property
    def exit_value(self):
        """Exit threshold for %B (default: 0.5)"""
        return self._exit_value.Value

    @exit_value.setter
    def exit_value(self, value):
        self._exit_value.Value = value

    @property
    def stop_loss_percent(self):
        """Stop-loss as percentage from entry price (default: 2%)"""
        return self._stop_loss_percent.Value

    @stop_loss_percent.setter
    def stop_loss_percent(self, value):
        self._stop_loss_percent.Value = value

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
        super(bollinger_percent_b_strategy, self).OnReseted()

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(bollinger_percent_b_strategy, self).OnStarted(time)

        # Create indicators
        bollinger = BollingerBands()
        bollinger.Length = self.bollinger_period
        bollinger.Width = self.bollinger_deviation

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bollinger, self.ProcessCandle).Start()

        # Configure chart
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)
            self.DrawOwnTrades(area)

        # Setup protection with stop-loss
        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(self.stop_loss_percent, UnitTypes.Percent)
        )
    def ProcessCandle(self, candle, bollinger_value):
        """
        Process candle and calculate Bollinger %B
        
        :param candle: The processed candle message.
        :param bollinger_value: The current value of the Bollinger Bands indicator.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Extract Bollinger Bands values
        try:
            if hasattr(bollinger_value, 'UpBand') and bollinger_value.UpBand is not None:
                upper_band = float(bollinger_value.UpBand)
            else:
                return
                
            if hasattr(bollinger_value, 'LowBand') and bollinger_value.LowBand is not None:
                lower_band = float(bollinger_value.LowBand)
            else:
                return
        except:
            # If we can't extract values, skip this candle
            return

        # Calculate Bollinger %B: (Price - Lower Band) / (Upper Band - Lower Band)
        percent_b = 0.0
        if upper_band != lower_band:
            percent_b = (candle.ClosePrice - lower_band) / (upper_band - lower_band)

        if self.Position == 0:
            # No position - check for entry signals
            if percent_b < 0:
                # Price below lower band (%B < 0) - buy (long)
                self.BuyMarket(self.Volume)
                self.LogInfo("Buy signal: Bollinger %B = {0:F3} (price below lower band)".format(percent_b))
            elif percent_b > 1:
                # Price above upper band (%B > 1) - sell (short)
                self.SellMarket(self.Volume)
                self.LogInfo("Sell signal: Bollinger %B = {0:F3} (price above upper band)".format(percent_b))
        elif self.Position > 0:
            # Long position - check for exit signal
            if percent_b > self.exit_value:
                # %B moved above exit threshold - exit long
                self.SellMarket(self.Position)
                self.LogInfo("Exit long: Bollinger %B = {0:F3} above exit threshold {1:F3}".format(
                    percent_b, self.exit_value))
        elif self.Position < 0:
            # Short position - check for exit signal
            if percent_b < self.exit_value:
                # %B moved below exit threshold - exit short
                self.BuyMarket(Math.Abs(self.Position))
                self.LogInfo("Exit short: Bollinger %B = {0:F3} below exit threshold {1:F3}".format(
                    percent_b, self.exit_value))

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return bollinger_percent_b_strategy()
