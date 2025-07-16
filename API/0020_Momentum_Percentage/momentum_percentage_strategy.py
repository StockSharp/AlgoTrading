import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import DataType, UnitTypes, Unit
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import Momentum, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class momentum_percentage_strategy(Strategy):
    """
    Strategy based on price momentum percentage change.
    It enters positions when momentum percentage exceeds threshold 
    and price is on the correct side of the moving average.
    
    See more examples: https://github.com/StockSharp/AlgoTrading
    """
    
    def __init__(self):
        super(momentum_percentage_strategy, self).__init__()
        
        # Initialize strategy parameters
        self._momentum_period = self.Param("MomentumPeriod", 10) \
            .SetDisplay("Momentum Period", "Period for momentum calculation", "Indicators")
        
        self._threshold_percent = self.Param("ThresholdPercent", 5.0) \
            .SetDisplay("Threshold %", "Momentum percentage threshold for entry", "Strategy")
        
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")
        
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def momentum_period(self):
        """Momentum period."""
        return self._momentum_period.Value

    @momentum_period.setter
    def momentum_period(self, value):
        self._momentum_period.Value = value

    @property
    def threshold_percent(self):
        """Threshold percentage for entry."""
        return self._threshold_percent.Value

    @threshold_percent.setter
    def threshold_percent(self, value):
        self._threshold_percent.Value = value

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
        super(momentum_percentage_strategy, self).OnReseted()

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(momentum_percentage_strategy, self).OnStarted(time)

        # Create indicators
        momentum = Momentum()
        momentum.Length = self.momentum_period
        
        sma = SimpleMovingAverage()
        sma.Length = 20

        # Subscribe to candles and bind the indicators
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(momentum, sma, self.ProcessCandle).Start()

        # Enable stop loss protection
        self.StartProtection(
            takeProfit=None,
            stopLoss=Unit(self.stop_loss_percent, UnitTypes.Percent)
        )
        # Setup chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, momentum)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, momentum_value, sma_value):
        """
        Processes each finished candle and executes momentum percentage logic.
        
        :param candle: The processed candle message.
        :param momentum_value: The current value of the Momentum indicator.
        :param sma_value: The current value of the SMA indicator.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Convert values to float
        momentum_decimal = float(momentum_value)
        sma_decimal = float(sma_value)

        # Calculate the percentage change
        # Momentum indicator returns the difference between current price and price N periods ago
        # To get percentage change: (momentum / (close - momentum)) * 100
        close_price = candle.ClosePrice
        previous_price = close_price - momentum_decimal
        
        if previous_price == 0:
            return  # Avoid division by zero
            
        percent_change = (momentum_decimal / previous_price) * 100

        # Entry logic
        if (percent_change > self.threshold_percent and 
            candle.ClosePrice > sma_decimal and self.Position <= 0):
            # Price increased by more than threshold percentage and is above MA - buy signal
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)
            self.LogInfo("Buy signal: Momentum {0:F2}% increase over {1} periods".format(
                percent_change, self.momentum_period))
        elif (percent_change < -self.threshold_percent and 
              candle.ClosePrice < sma_decimal and self.Position >= 0):
            # Price decreased by more than threshold percentage and is below MA - sell signal
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)
            self.LogInfo("Sell signal: Momentum {0:F2}% decrease over {1} periods".format(
                abs(percent_change), self.momentum_period))

        # Exit logic
        if self.Position > 0 and candle.ClosePrice < sma_decimal:
            # Exit long position when price falls below MA
            self.SellMarket(Math.Abs(self.Position))
            self.LogInfo("Exiting long position: Price fell below MA. Price: {0}, MA: {1}".format(
                candle.ClosePrice, sma_decimal))
        elif self.Position < 0 and candle.ClosePrice > sma_decimal:
            # Exit short position when price rises above MA
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo("Exiting short position: Price rose above MA. Price: {0}, MA: {1}".format(
                candle.ClosePrice, sma_decimal))

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return momentum_percentage_strategy()
